using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ENTcapture2.WinForms.Capture;

public sealed class VideoPlaybackService : IAsyncDisposable
{
    private readonly object _stateLock = new();
    private CancellationTokenSource? _cancellation;
    private Task? _playbackTask;
    private VideoCapture? _capture;
    private IReadOnlyList<string> _files = [];
    private IReadOnlyList<long> _segmentFrameCounts = [];
    private IReadOnlyList<TimeSpan> _segmentDurations = [];
    private int _fileIndex;
    private long _segmentStartFrame;
    private bool _isPaused;
    private TimeSpan? _pendingSeekTime;

    public event Action<Bitmap, PlaybackPosition>? FrameReady;

    public bool IsOpen => _capture is not null;

    public bool IsPaused
    {
        get
        {
            lock (_stateLock)
            {
                return _isPaused;
            }
        }
    }

    public async Task OpenAsync(string filePath)
    {
        await OpenAsync(new[] { filePath });
    }

    public async Task OpenAsync(IReadOnlyList<string> filePaths)
    {
        await CloseAsync();
        if (filePaths.Count == 0)
        {
            throw new ArgumentException("再生ファイルがありません。");
        }

        _files = filePaths;
        PlaybackFileInfo[] fileInfo = filePaths
            .Select(GetFileInfo)
            .ToArray();
        _segmentFrameCounts = fileInfo
            .Select(item => item.FrameCount)
            .ToArray();
        _segmentDurations = fileInfo
            .Select(item => item.Duration)
            .ToArray();
        _fileIndex = 0;
        _segmentStartFrame = 0;
        OpenSegment(0);
        if (_capture is null || !_capture.IsOpened())
        {
            _capture?.Dispose();
            _capture = null;
            throw new InvalidOperationException(
                $"一次動画を開けませんでした: {filePaths[0]}");
        }

        lock (_stateLock)
        {
            _isPaused = false;
            _pendingSeekTime = null;
        }

        _cancellation = new CancellationTokenSource();
        _playbackTask = Task.Run(
            () => PlaybackLoopAsync(_cancellation.Token),
            _cancellation.Token);
    }

    public void TogglePause()
    {
        lock (_stateLock)
        {
            _isPaused = !_isPaused;
        }
    }

    public void Pause()
    {
        lock (_stateLock)
        {
            _isPaused = true;
        }
    }

    public void Seek(TimeSpan time)
    {
        lock (_stateLock)
        {
            _pendingSeekTime = time < TimeSpan.Zero
                ? TimeSpan.Zero
                : time;
            _isPaused = true;
        }
    }

    public async Task CloseAsync()
    {
        CancellationTokenSource? cancellation = _cancellation;
        _cancellation = null;
        cancellation?.Cancel();

        try
        {
            _capture?.Release();
            Task playbackTask = _playbackTask ?? Task.CompletedTask;
            Task completed = await Task.WhenAny(
                playbackTask,
                Task.Delay(TimeSpan.FromSeconds(2)));
            if (completed == playbackTask)
            {
                await playbackTask;
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation?.Dispose();
            _playbackTask = null;
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            _files = [];
            _segmentFrameCounts = [];
            _segmentDurations = [];
            _fileIndex = 0;
            _segmentStartFrame = 0;
            _pendingSeekTime = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }

    private async Task PlaybackLoopAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_capture is not null);

        double fps = _capture.Get(VideoCaptureProperties.Fps);
        if (fps <= 0 || double.IsNaN(fps))
        {
            fps = 30;
        }

        long frameCount = Math.Max(1, _segmentFrameCounts.Sum());
        TimeSpan frameInterval = TimeSpan.FromSeconds(1.0 / fps);
        TimeSpan totalDuration = TimeSpan.FromTicks(
            _segmentDurations.Sum(item => item.Ticks));
        double baseTimestampMilliseconds = -1;
        long playbackFrameOrdinal = 0;
        var playbackClock = new Stopwatch();
        var playbackFpsClock = Stopwatch.StartNew();
        int renderedFrames = 0;
        double playbackFramesPerSecond = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            bool paused;
            TimeSpan? seekTime;
            lock (_stateLock)
            {
                paused = _isPaused;
                seekTime = _pendingSeekTime;
                _pendingSeekTime = null;
            }

            if (seekTime.HasValue)
            {
                TimeSpan targetTime = seekTime.Value > totalDuration
                    ? totalDuration
                    : seekTime.Value;
                int targetSegment = FindSegment(targetTime);
                if (targetSegment != _fileIndex)
                {
                    OpenSegment(targetSegment);
                }

                TimeSpan segmentStartTime = TimeSpan.FromTicks(
                    _segmentDurations
                        .Take(_fileIndex)
                        .Sum(item => item.Ticks));
                TimeSpan segmentTime = targetTime - segmentStartTime;
                if (segmentTime < TimeSpan.Zero)
                {
                    segmentTime = TimeSpan.Zero;
                }

                _capture.Set(
                    VideoCaptureProperties.PosMsec,
                    segmentTime.TotalMilliseconds);
                paused = true;
                lock (_stateLock)
                {
                    _isPaused = true;
                }

                baseTimestampMilliseconds = -1;
                playbackFrameOrdinal = 0;
                playbackClock.Reset();
            }
            else if (paused)
            {
                baseTimestampMilliseconds = -1;
                playbackFrameOrdinal = 0;
                playbackClock.Reset();
                await Task.Delay(20, cancellationToken);
                continue;
            }

            var frame = new Mat();
            if (!_capture.Read(frame) || frame.Empty())
            {
                frame.Dispose();
                if (_fileIndex + 1 < _files.Count)
                {
                    OpenSegment(_fileIndex + 1);
                    baseTimestampMilliseconds = -1;
                    playbackFrameOrdinal = 0;
                    playbackClock.Reset();
                    continue;
                }

                lock (_stateLock)
                {
                    _isPaused = true;
                }
                continue;
            }

            using (frame)
            {
                double timestampMilliseconds =
                    _capture.Get(VideoCaptureProperties.PosMsec);
                if (!playbackClock.IsRunning)
                {
                    playbackClock.Restart();
                }

                TimeSpan targetElapsed;
                if (timestampMilliseconds > 0 &&
                    !double.IsNaN(timestampMilliseconds))
                {
                    if (baseTimestampMilliseconds < 0)
                    {
                        baseTimestampMilliseconds = timestampMilliseconds;
                    }

                    targetElapsed = TimeSpan.FromMilliseconds(
                        Math.Max(
                            0,
                            timestampMilliseconds - baseTimestampMilliseconds));
                }
                else
                {
                    targetElapsed = TimeSpan.FromTicks(
                        frameInterval.Ticks * playbackFrameOrdinal);
                }

                TimeSpan delay = targetElapsed - playbackClock.Elapsed;
                if (delay > TimeSpan.FromMilliseconds(1))
                {
                    await Task.Delay(delay, cancellationToken);
                }

                renderedFrames++;
                playbackFrameOrdinal++;
                if (playbackFpsClock.ElapsedMilliseconds >= 1000)
                {
                    playbackFramesPerSecond =
                        renderedFrames /
                        playbackFpsClock.Elapsed.TotalSeconds;
                    renderedFrames = 0;
                    playbackFpsClock.Restart();
                }

                Bitmap bitmap = BitmapConverter.ToBitmap(frame);
                long currentFrame = Math.Max(
                    0,
                    _segmentStartFrame +
                    (long)_capture.Get(VideoCaptureProperties.PosFrames) - 1);
                TimeSpan currentTime = TimeSpan.FromTicks(
                    _segmentDurations
                        .Take(_fileIndex)
                        .Sum(item => item.Ticks)) +
                    TimeSpan.FromMilliseconds(
                        Math.Max(0, timestampMilliseconds));
                FrameReady?.Invoke(
                    bitmap,
                    new PlaybackPosition(
                        currentFrame,
                        frameCount,
                        fps,
                        playbackFramesPerSecond,
                        currentTime,
                        totalDuration));
            }
        }
    }

    private void OpenSegment(int index)
    {
        _capture?.Release();
        _capture?.Dispose();
        _fileIndex = index;
        _segmentStartFrame = _segmentFrameCounts
            .Take(index)
            .Sum();
        _capture = new VideoCapture(_files[index]);
        if (!_capture.IsOpened())
        {
            throw new InvalidOperationException(
                $"一次動画を開けませんでした: {_files[index]}");
        }
    }

    private int FindSegment(TimeSpan globalTime)
    {
        TimeSpan offset = TimeSpan.Zero;
        for (int index = 0; index < _segmentFrameCounts.Count; index++)
        {
            offset += _segmentDurations[index];
            if (globalTime < offset)
            {
                return index;
            }
        }

        return Math.Max(0, _segmentFrameCounts.Count - 1);
    }

    private static PlaybackFileInfo GetFileInfo(string filePath)
    {
        using var capture = new VideoCapture(filePath);
        long frameCount = Math.Max(
            1,
            (long)capture.Get(VideoCaptureProperties.FrameCount));
        double fps = capture.Get(VideoCaptureProperties.Fps);
        TimeSpan duration =
            fps > 0 && !double.IsNaN(fps)
                ? TimeSpan.FromSeconds(frameCount / fps)
                : TimeSpan.Zero;
        return new PlaybackFileInfo(frameCount, duration);
    }

    private sealed record PlaybackFileInfo(
        long FrameCount,
        TimeSpan Duration);
}

public sealed record PlaybackPosition(
    long CurrentFrame,
    long FrameCount,
    double FramesPerSecond,
    double PlaybackFramesPerSecond,
    TimeSpan CurrentTime,
    TimeSpan TotalTime);
