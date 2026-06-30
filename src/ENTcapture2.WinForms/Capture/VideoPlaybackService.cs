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
    private long? _pendingSeekFrame;

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
            _pendingSeekFrame = null;
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
        SeekFrame(FindFrameForTime(time));
    }

    public void SeekFrame(long frame)
    {
        lock (_stateLock)
        {
            _pendingSeekFrame = Math.Max(0, frame);
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
            _pendingSeekFrame = null;
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
            long? seekFrame;
            lock (_stateLock)
            {
                paused = _isPaused;
                seekFrame = _pendingSeekFrame;
                _pendingSeekFrame = null;
            }

            if (seekFrame.HasValue)
            {
                long targetFrame = Math.Clamp(
                    seekFrame.Value,
                    0,
                    frameCount - 1);
                int targetSegment = FindSegmentByFrame(targetFrame);
                if (targetSegment != _fileIndex)
                {
                    OpenSegment(targetSegment);
                }

                long segmentFrame = Math.Clamp(
                    targetFrame - _segmentStartFrame,
                    0,
                    _segmentFrameCounts[_fileIndex] - 1);
                _capture.Set(
                    VideoCaptureProperties.PosFrames,
                    segmentFrame);
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
                currentFrame = Math.Clamp(currentFrame, 0, frameCount - 1);
                TimeSpan currentTime = GetTimeForFrame(
                    currentFrame,
                    frameCount,
                    totalDuration,
                    fps);
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

    private long FindFrameForTime(TimeSpan globalTime)
    {
        long frameCount = Math.Max(1, _segmentFrameCounts.Sum());
        TimeSpan totalDuration = TimeSpan.FromTicks(
            _segmentDurations.Sum(item => item.Ticks));
        if (globalTime <= TimeSpan.Zero ||
            totalDuration <= TimeSpan.Zero ||
            frameCount <= 1)
        {
            return 0;
        }

        return Math.Clamp(
            (long)Math.Round(
                globalTime.Ticks /
                (double)totalDuration.Ticks *
                (frameCount - 1)),
            0,
            frameCount - 1);
    }

    private int FindSegmentByFrame(long globalFrame)
    {
        long offset = 0;
        for (int index = 0; index < _segmentFrameCounts.Count; index++)
        {
            offset += _segmentFrameCounts[index];
            if (globalFrame < offset)
            {
                return index;
            }
        }

        return Math.Max(0, _segmentFrameCounts.Count - 1);
    }

    private static TimeSpan GetTimeForFrame(
        long currentFrame,
        long frameCount,
        TimeSpan totalDuration,
        double framesPerSecond)
    {
        if (totalDuration > TimeSpan.Zero && frameCount > 1)
        {
            return TimeSpan.FromTicks(Math.Clamp(
                (long)Math.Round(
                    currentFrame /
                    (double)(frameCount - 1) *
                    totalDuration.Ticks),
                0,
                totalDuration.Ticks));
        }

        if (framesPerSecond > 0 && !double.IsNaN(framesPerSecond))
        {
            return TimeSpan.FromSeconds(currentFrame / framesPerSecond);
        }

        return TimeSpan.Zero;
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
