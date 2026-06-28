using System.Diagnostics;
using System.Threading.Channels;
using GitHub.secile.Video;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ENTcapture2.WinForms.Capture;

public sealed class CameraCaptureService : IAsyncDisposable
{
    private const int RecordingQueueCapacity = 600;
    private const int MaximumBestSnapshotWindowMilliseconds = 1000;

    private readonly object _imageLock = new();
    private readonly object _optionsLock = new();
    private readonly Queue<SnapshotCandidate> _snapshotCandidates = new();
    private CancellationTokenSource? _cancellation;
    private Task? _captureTask;
    private Task? _bitmapIngestTask;
    private Task? _processingTask;
    private Task? _recordingTask;
    private VideoCapture? _capture;
    private UsbCamera? _usbCamera;
    private Bitmap? _latestImage;
    private Mat? _latestSourceFrame;
    private Channel<Bitmap>? _bitmapChannel;
    private Channel<Mat>? _frameChannel;
    private Channel<Mat>? _recordingChannel;
    private RecordingOptions? _recordingOptions;
    private ProcessingOptions _options = ProcessingOptions.Default;
    private long _receivedFrames;
    private long _displayedFrames;
    private long _droppedFrames;
    private int _maximumPreviewFramesPerSecond;
    private volatile bool _retainUnprocessedSnapshot;

    public event Action<Bitmap>? FrameReady;

    public event Action<CaptureStatistics>? StatisticsUpdated;

    public event Action<Exception>? CaptureFaulted;

    public bool IsRunning => _cancellation is not null;

    public string? LastRecordingPath { get; private set; }

    public IReadOnlyList<string> LastRecordingPaths =>
        _lastRecordingPaths.ToArray();

    public Exception? LastRecordingError { get; private set; }

    public string? LastRecordingWarning { get; private set; }

    public string? LastRecordingEncoder { get; private set; }

    public BestSnapshotSelection? LastBestSnapshotSelection { get; private set; }

    private readonly List<string> _lastRecordingPaths = [];

    public async Task StartAsync(
        int deviceIndex,
        int width,
        int height,
        int framesPerSecond,
        string pixelFormat,
        RecordingOptions? recordingOptions = null,
        int maximumPreviewFramesPerSecond = 0,
        CancellationToken cancellationToken = default)
    {
        ENTcapture2.Core.Services.DebugLogger.Debug(
            $"CameraCaptureService.StartAsync: deviceIndex={deviceIndex}, " +
            $"resolution={width}x{height}@{framesPerSecond}fps, " +
            $"pixelFormat={pixelFormat}, recording={recordingOptions is not null}");

        try
        {
            await StopAsync();

            _receivedFrames = 0;
            _displayedFrames = 0;
            _droppedFrames = 0;
            LastRecordingPath = null;
            _lastRecordingPaths.Clear();
            LastRecordingError = null;
            LastRecordingWarning = null;
            LastRecordingEncoder = null;
            _recordingOptions = recordingOptions;
            _maximumPreviewFramesPerSecond =
                Math.Clamp(maximumPreviewFramesPerSecond, 0, 240);
            _frameChannel = Channel.CreateBounded<Mat>(
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true
                });
            _bitmapChannel = Channel.CreateBounded<Bitmap>(
                new BoundedChannelOptions(2)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
            if (recordingOptions is not null)
            {
                _recordingChannel = Channel.CreateBounded<Mat>(
                    new BoundedChannelOptions(RecordingQueueCapacity)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = true
                    });
            }

            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = _cancellation.Token;
            UsbCamera.VideoFormat format =
                SelectUsbCameraFormat(
                    deviceIndex,
                    width,
                    height,
                    framesPerSecond,
                    pixelFormat);
            _usbCamera = new UsbCamera(deviceIndex, format);
            ENTcapture2.Core.Services.DebugLogger.Info($"CameraCaptureService: Device initialized, starting capture");
            _usbCamera.PreviewCaptured = UsbCamera_PreviewCaptured;
            _usbCamera.Start();

            _captureTask = Task.Run(() => StatisticsLoopAsync(token), token);
            _bitmapIngestTask = Task.Run(() => BitmapIngestLoopAsync(token), token);
            _processingTask = Task.Run(() => ProcessingLoopAsync(token), token);
            _recordingTask = _recordingChannel is null
                ? null
                : Task.Run(() => RecordingLoopAsync(), CancellationToken.None);

            ENTcapture2.Core.Services.DebugLogger.Info("CameraCaptureService.StartAsync: Capture started successfully");
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error("CameraCaptureService.StartAsync: Failed to start capture", exception);
            try
            {
                await StopAsync();
                CleanupPartialStartResources();
            }
            catch (Exception cleanupException)
            {
                ENTcapture2.Core.Services.DebugLogger.Error(
                    "CameraCaptureService.StartAsync: Cleanup after failed start failed",
                    cleanupException);
            }

            throw;
        }
    }

    private void CleanupPartialStartResources()
    {
        try
        {
            _usbCamera?.Stop();
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.CleanupPartialStartResources: Error stopping USB camera",
                exception);
        }

        try
        {
            _usbCamera?.Release();
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.CleanupPartialStartResources: Error releasing USB camera",
                exception);
        }

        _usbCamera = null;
        _bitmapChannel?.Writer.TryComplete();
        _frameChannel?.Writer.TryComplete();
        _recordingChannel?.Writer.TryComplete();
        _bitmapChannel = null;
        _frameChannel = null;
        _recordingChannel = null;
        _recordingOptions = null;
        _cancellation?.Dispose();
        _cancellation = null;
        lock (_imageLock)
        {
            ClearSnapshotCandidates();
        }
    }

    private static UsbCamera.VideoFormat SelectUsbCameraFormat(
        int deviceIndex,
        int width,
        int height,
        int framesPerSecond,
        string pixelFormat)
    {
        UsbCamera.VideoFormat[] formats = UsbCamera.GetVideoFormat(deviceIndex);
        if (formats.Length == 0)
        {
            return UsbCamera.VideoFormat.Default;
        }

        string normalizedPixelFormat = NormalizePixelFormat(pixelFormat);
        return formats
            .Where(format =>
                (width <= 0 || format.Size.Width == width) &&
                (height <= 0 || format.Size.Height == height))
            .OrderBy(format =>
                string.Equals(
                    NormalizePixelFormat(format.SubType),
                    normalizedPixelFormat,
                    StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .ThenBy(format =>
                framesPerSecond <= 0
                    ? 0
                    : Math.Abs(GetFramesPerSecond(format) - framesPerSecond))
            .ThenBy(format => GetPixelFormatPriority(format.SubType))
            .FirstOrDefault()
            ?? formats[0];
    }

    private static double GetFramesPerSecond(UsbCamera.VideoFormat format)
    {
        return format.TimePerFrame > 0
            ? 10_000_000.0 / format.TimePerFrame
            : 0;
    }

    private static string NormalizePixelFormat(string pixelFormat)
    {
        string normalized = pixelFormat
            .Replace("[", string.Empty)
            .Replace("]", string.Empty)
            .Replace("MEDIASUBTYPE_", string.Empty)
            .Trim()
            .ToUpperInvariant();
        return normalized switch
        {
            "MJPEG" => "MJPG",
            "YUYV" => "YUY2",
            _ => normalized
        };
    }

    private static int GetPixelFormatPriority(string pixelFormat)
    {
        return NormalizePixelFormat(pixelFormat) switch
        {
            "MJPG" => 0,
            "H264" or "H265" or "HEVC" => 1,
            "YUY2" or "UYVY" => 2,
            "RGB24" or "RGB32" => 3,
            _ => 4
        };
    }

    private static bool TryGetFourCc(string pixelFormat, out int fourCc)
    {
        fourCc = 0;
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return false;
        }

        string normalized = pixelFormat.Trim().ToUpperInvariant();
        normalized = normalized switch
        {
            "RGB24" => "BGR3",
            "RGB32" => "BGR4",
            _ => normalized
        };

        if (normalized.Length != 4 ||
            normalized.Any(character => character is < ' ' or > '~'))
        {
            return false;
        }

        fourCc = VideoWriter.FourCC(
            normalized[0],
            normalized[1],
            normalized[2],
            normalized[3]);
        return true;
    }

    public async Task StopAsync()
    {
        ENTcapture2.Core.Services.DebugLogger.Debug("CameraCaptureService.StopAsync: Stopping capture");

        CancellationTokenSource? cancellation = _cancellation;
        if (cancellation is null)
        {
            return;
        }

        _cancellation = null;
        cancellation.Cancel();
        try
        {
            ENTcapture2.Core.Services.DebugLogger.Debug("CameraCaptureService.StopAsync: Stopping USB camera");
            _usbCamera?.Stop();
            _usbCamera?.Release();
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error("CameraCaptureService.StopAsync: Error stopping USB camera", exception);
        }
        finally
        {
            _usbCamera = null;
        }

        try
        {
            Task captureTask = _captureTask ?? Task.CompletedTask;
            Task completedTask = await Task.WhenAny(
                captureTask,
                Task.Delay(TimeSpan.FromMilliseconds(1500)));
            if (!ReferenceEquals(completedTask, captureTask))
            {
                ENTcapture2.Core.Services.DebugLogger.Warning("CameraCaptureService.StopAsync: Capture task timeout, forcing release");
                _capture?.Release();
            }

            await captureTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error("CameraCaptureService.StopAsync: Error stopping capture task", exception);
        }
        finally
        {
            _bitmapChannel?.Writer.TryComplete();
            _frameChannel?.Writer.TryComplete();
            _recordingChannel?.Writer.TryComplete();
        }

        try
        {
            await Task.WhenAll(
                _bitmapIngestTask ?? Task.CompletedTask,
                _processingTask ?? Task.CompletedTask,
                _recordingTask ?? Task.CompletedTask);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error("CameraCaptureService.StopAsync: Error stopping processing tasks", exception);
        }
        finally
        {
            cancellation.Dispose();
            _captureTask = null;
            _bitmapIngestTask = null;
            _processingTask = null;
            _recordingTask = null;
            _bitmapChannel = null;
            _frameChannel = null;
            _recordingChannel = null;
            _recordingOptions = null;
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            lock (_imageLock)
            {
                ClearSnapshotCandidates();
            }

            ENTcapture2.Core.Services.DebugLogger.Info("CameraCaptureService.StopAsync: Capture stopped");
        }
    }

    public void UpdateProcessingOptions(
        int red,
        int green,
        int blue,
        double gamma,
        bool flipHorizontal,
        bool flipVertical)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdateProcessingOptions R={red} G={green} B={blue} Gamma={gamma} FlipH={flipHorizontal} FlipV={flipVertical}");
        lock (_optionsLock)
        {
            _options = new ProcessingOptions(
                Math.Clamp(red, 0, 510),
                Math.Clamp(green, 0, 510),
                Math.Clamp(blue, 0, 510),
                Math.Clamp(gamma, 0.1, 3.0),
                flipHorizontal,
                flipVertical);
        }
    }

    public Bitmap? GetSnapshot()
    {
        lock (_imageLock)
        {
            return _latestImage is null ? null : (Bitmap)_latestImage.Clone();
        }
    }

    public Bitmap? GetBestSnapshot(
        int windowMilliseconds,
        string? roiText)
    {
        int window = Math.Clamp(
            windowMilliseconds,
            0,
            MaximumBestSnapshotWindowMilliseconds);
        LastBestSnapshotSelection = null;
        if (window <= 0)
        {
            return GetSnapshot();
        }

        DateTime cutoff = DateTime.UtcNow.AddMilliseconds(-window);
        var candidates = new List<SnapshotCandidate>();
        lock (_imageLock)
        {
            PruneSnapshotCandidates(
                DateTime.UtcNow.AddMilliseconds(
                    -MaximumBestSnapshotWindowMilliseconds));
            foreach (SnapshotCandidate candidate in _snapshotCandidates)
            {
                if (candidate.CapturedAtUtc >= cutoff)
                {
                    candidates.Add(new SnapshotCandidate(
                        candidate.Frame.Clone(),
                        candidate.CapturedAtUtc));
                }
            }
        }

        if (candidates.Count == 0)
        {
            return GetSnapshot();
        }

        try
        {
            SnapshotCandidate? bestCandidate = null;
            Rect scoringRoi;
            double latestScore = CalculateSharpnessScore(
                candidates[^1].Frame,
                roiText,
                out scoringRoi);
            var scores = new List<SnapshotCandidateScore>();
            double bestScore = double.MinValue;
            for (int index = 0; index < candidates.Count; index++)
            {
                SnapshotCandidate candidate = candidates[index];
                double score = index == candidates.Count - 1
                    ? latestScore
                    : CalculateSharpnessScore(
                        candidate.Frame,
                        roiText,
                        out _);
                int ageMilliseconds = (int)Math.Round(
                    (DateTime.UtcNow - candidate.CapturedAtUtc)
                    .TotalMilliseconds);
                scores.Add(new SnapshotCandidateScore(
                    index + 1,
                    ageMilliseconds,
                    score));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate is null)
            {
                return GetSnapshot();
            }

            LastBestSnapshotSelection = new BestSnapshotSelection(
                candidates.Count,
                (int)Math.Round(
                    (DateTime.UtcNow - bestCandidate.CapturedAtUtc)
                    .TotalMilliseconds),
                bestScore,
                latestScore,
                !ReferenceEquals(bestCandidate, candidates[^1]));
            LogBestSnapshotSelection(
                window,
                roiText,
                scoringRoi,
                scores,
                LastBestSnapshotSelection);
            return BitmapConverter.ToBitmap(bestCandidate.Frame);
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.GetBestSnapshot: Failed to select best frame",
                exception);
            return GetSnapshot();
        }
        finally
        {
            foreach (SnapshotCandidate candidate in candidates)
            {
                candidate.Frame.Dispose();
            }
        }
    }

    public Bitmap? GetUnprocessedSnapshot()
    {
        lock (_imageLock)
        {
            return _latestSourceFrame is null
                ? null
                : BitmapConverter.ToBitmap(_latestSourceFrame);
        }
    }

    public void SetUnprocessedSnapshotEnabled(bool enabled)
    {
        lock (_imageLock)
        {
            _retainUnprocessedSnapshot = enabled;
            if (!enabled)
            {
                _latestSourceFrame?.Dispose();
                _latestSourceFrame = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        lock (_imageLock)
        {
            _latestImage?.Dispose();
            _latestImage = null;
            _latestSourceFrame?.Dispose();
            _latestSourceFrame = null;
            ClearSnapshotCandidates();
        }
    }

    private void UsbCamera_PreviewCaptured(Bitmap bitmap)
    {
        try
        {
            if (_cancellation is null ||
                _bitmapChannel is null)
            {
                bitmap.Dispose();
                return;
            }

            Interlocked.Increment(ref _receivedFrames);
            if (!_bitmapChannel.Writer.TryWrite(bitmap))
            {
                bitmap.Dispose();
                Interlocked.Increment(ref _droppedFrames);
                long droppedCount = Interlocked.Read(ref _droppedFrames);
                if (droppedCount % 30 == 0)
                {
                    ENTcapture2.Core.Services.DebugLogger.Warning(
                        $"CameraCaptureService: Frame drop detected. Received={Interlocked.Read(ref _receivedFrames)}, " +
                        $"Dropped={droppedCount}");
                }
            }
        }
        catch (Exception exception)
        {
            bitmap.Dispose();
            LastRecordingError ??= exception;
            ENTcapture2.Core.Services.DebugLogger.Error("CameraCaptureService.UsbCamera_PreviewCaptured: Error processing frame", exception);
        }
    }

    private async Task BitmapIngestLoopAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_bitmapChannel is not null);
        try
        {
            await foreach (Bitmap bitmap in _bitmapChannel.Reader.ReadAllAsync(cancellationToken))
            {
                using (bitmap)
                using (Mat frame = BitmapConverter.ToMat(bitmap))
                {
                    EnqueueCapturedFrame(frame);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.BitmapIngestLoopAsync: Error ingesting frames",
                exception);
            ReportCaptureFault(exception);
        }
    }

    private void EnqueueCapturedFrame(Mat frame)
    {
        if (_retainUnprocessedSnapshot)
        {
            lock (_imageLock)
            {
                if (_retainUnprocessedSnapshot)
                {
                    Mat? previous = _latestSourceFrame;
                    _latestSourceFrame = frame.Clone();
                    previous?.Dispose();
                }
            }
        }

        if (_recordingChannel is not null)
        {
            Mat recordingFrame = frame.Clone();
            if (!_recordingChannel.Writer.TryWrite(recordingFrame))
            {
                if (_recordingChannel.Reader.TryRead(out Mat? staleRecordingFrame))
                {
                    staleRecordingFrame.Dispose();
                    Interlocked.Increment(ref _droppedFrames);
                }

                if (!_recordingChannel.Writer.TryWrite(recordingFrame))
                {
                    recordingFrame.Dispose();
                    Interlocked.Increment(ref _droppedFrames);
                }
            }
        }

        Mat previewFrame = frame.Clone();
        if (!_frameChannel!.Writer.TryWrite(previewFrame))
        {
            if (_frameChannel.Reader.TryRead(out Mat? staleFrame))
            {
                staleFrame.Dispose();
                Interlocked.Increment(ref _droppedFrames);
            }

            if (!_frameChannel.Writer.TryWrite(previewFrame))
            {
                previewFrame.Dispose();
                Interlocked.Increment(ref _droppedFrames);
            }
        }
    }

    private async Task StatisticsLoopAsync(CancellationToken cancellationToken)
    {
        long previousReceived = 0;
        long previousDisplayed = 0;
        long previousDropped = 0;
        int stagnantInputSeconds = 0;
        var statisticsClock = Stopwatch.StartNew();

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            long receivedCount = Interlocked.Read(ref _receivedFrames);
            long displayedCount = Interlocked.Read(ref _displayedFrames);
            long droppedCount = Interlocked.Read(ref _droppedFrames);
            double elapsedSeconds = statisticsClock.Elapsed.TotalSeconds;
            long newReceivedFrames = receivedCount - previousReceived;
            if (newReceivedFrames <= 0)
            {
                stagnantInputSeconds++;
                ENTcapture2.Core.Services.DebugLogger.Warning(
                    "CameraCaptureService: No input frames received. " +
                    $"StagnantSeconds={stagnantInputSeconds}, " +
                    $"Received={receivedCount}, Displayed={displayedCount}");
                if (stagnantInputSeconds >= 3)
                {
                    var exception = new IOException(
                        "No input frames were received from the camera for 3 seconds.");
                    ReportCaptureFault(exception);
                    return;
                }
            }
            else
            {
                stagnantInputSeconds = 0;
            }

            // Log significant frame drop events
            if (droppedCount > previousDropped)
            {
                long newDrops = droppedCount - previousDropped;
                ENTcapture2.Core.Services.DebugLogger.Warning(
                    $"CameraCaptureService: Frame drop. " +
                    $"Received={receivedCount}, Displayed={displayedCount}, " +
                    $"Total Dropped={droppedCount}, New Drops={newDrops}");
            }

            StatisticsUpdated?.Invoke(
                new CaptureStatistics(
                    (receivedCount - previousReceived) / elapsedSeconds,
                    (displayedCount - previousDisplayed) / elapsedSeconds,
                    droppedCount));

            previousReceived = receivedCount;
            previousDisplayed = displayedCount;
            previousDropped = droppedCount;
            statisticsClock.Restart();
        }
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_capture is not null);
        Debug.Assert(_frameChannel is not null);

        var statisticsClock = Stopwatch.StartNew();
        long previousReceived = 0;
        long previousDisplayed = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var frame = new Mat();
            bool received = _capture.Read(frame);
            if (!received || frame.Empty())
            {
                frame.Dispose();
                await Task.Delay(5, cancellationToken);
                continue;
            }

            Interlocked.Increment(ref _receivedFrames);
            if (_retainUnprocessedSnapshot)
            {
                lock (_imageLock)
                {
                    if (_retainUnprocessedSnapshot)
                    {
                        Mat? previous = _latestSourceFrame;
                        _latestSourceFrame = frame.Clone();
                        previous?.Dispose();
                    }
                }
            }
            if (_recordingChannel is not null)
            {
                Mat recordingFrame = frame.Clone();
                if (!_recordingChannel.Writer.TryWrite(recordingFrame))
                {
                    if (_recordingChannel.Reader.TryRead(out Mat? staleRecordingFrame))
                    {
                        staleRecordingFrame.Dispose();
                        Interlocked.Increment(ref _droppedFrames);
                    }

                    if (!_recordingChannel.Writer.TryWrite(recordingFrame))
                    {
                        recordingFrame.Dispose();
                        Interlocked.Increment(ref _droppedFrames);
                    }
                }
            }

            if (!_frameChannel.Writer.TryWrite(frame))
            {
                if (_frameChannel.Reader.TryRead(out Mat? staleFrame))
                {
                    staleFrame.Dispose();
                    Interlocked.Increment(ref _droppedFrames);
                }

                if (!_frameChannel.Writer.TryWrite(frame))
                {
                    frame.Dispose();
                    Interlocked.Increment(ref _droppedFrames);
                }
            }

            if (statisticsClock.ElapsedMilliseconds < 1000)
            {
                continue;
            }

            long receivedCount = Interlocked.Read(ref _receivedFrames);
            long displayedCount = Interlocked.Read(ref _displayedFrames);
            double elapsedSeconds = statisticsClock.Elapsed.TotalSeconds;
            StatisticsUpdated?.Invoke(
                new CaptureStatistics(
                    (receivedCount - previousReceived) / elapsedSeconds,
                    (displayedCount - previousDisplayed) / elapsedSeconds,
                    Interlocked.Read(ref _droppedFrames)));

            previousReceived = receivedCount;
            previousDisplayed = displayedCount;
            statisticsClock.Restart();
        }
    }

    private async Task ProcessingLoopAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_frameChannel is not null);
        var previewClock = Stopwatch.StartNew();
        long processedCount = 0;

        try
        {
            await foreach (Mat source in _frameChannel.Reader.ReadAllAsync(cancellationToken))
            {
                using (source)
                using (Mat processed = ApplyProcessing(source))
                {
                    lock (_imageLock)
                    {
                        AddSnapshotCandidate(processed, DateTime.UtcNow);
                    }

                    if (_maximumPreviewFramesPerSecond > 0)
                    {
                        double minimumMilliseconds =
                            1000.0 / _maximumPreviewFramesPerSecond;
                        double remaining =
                            minimumMilliseconds -
                            previewClock.Elapsed.TotalMilliseconds;
                        if (remaining > 1)
                        {
                            await Task.Delay(
                                TimeSpan.FromMilliseconds(remaining),
                                cancellationToken);
                        }

                        previewClock.Restart();
                    }

                    Bitmap bitmap = BitmapConverter.ToBitmap(processed);
                    Bitmap eventBitmap = (Bitmap)bitmap.Clone();

                    lock (_imageLock)
                    {
                        Bitmap? previous = _latestImage;
                        _latestImage = bitmap;
                        previous?.Dispose();
                    }

                    Interlocked.Increment(ref _displayedFrames);
                    processedCount++;
                    FrameReady?.Invoke(eventBitmap);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception exception)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                $"CameraCaptureService.ProcessingLoopAsync: Error processing frames. Processed={processedCount}", exception);
            LastRecordingError ??= exception;
            ReportCaptureFault(exception);
        }
    }

    private async Task RecordingLoopAsync()
    {
        Debug.Assert(_recordingChannel is not null);
        Debug.Assert(_recordingOptions is not null);

        VideoWriter? writer = null;
        FfmpegVideoWriter? ffmpegWriter = null;
        Stopwatch? segmentClock = null;
        int segmentNumber = 1;
        string sessionTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        try
        {
            await foreach (Mat source in _recordingChannel.Reader.ReadAllAsync())
            {
                using (source)
                using (Mat processed = ApplyProcessing(source))
                {
                    RecordingOptions options = _recordingOptions;
                    bool segmentExpired =
                        options.SegmentMinutes > 0 &&
                        segmentClock?.Elapsed >=
                        TimeSpan.FromMinutes(options.SegmentMinutes);
                    if ((writer is not null || ffmpegWriter is not null) &&
                        segmentExpired)
                    {
                        writer?.Release();
                        writer?.Dispose();
                        writer = null;
                        ffmpegWriter?.Dispose();
                        ffmpegWriter = null;
                        segmentClock = null;
                        segmentNumber++;
                    }

                    if (writer is null && ffmpegWriter is null)
                    {
                        Directory.CreateDirectory(options.Directory);
                        string codec = NormalizeCodec(options.Codec);
                        FfmpegEncoderSelection? ffmpegEncoder = null;
                        if (codec is "H264" or "MJPG")
                        {
                            try
                            {
                                ffmpegEncoder = codec == "H264"
                                    ? FfmpegRuntime.SelectH264Encoder(
                                        options.H264EncoderPreference)
                                    : FfmpegRuntime.SelectMjpegEncoder();
                                if (ffmpegEncoder.UsedFallback)
                                {
                                    LastRecordingWarning =
                                        ffmpegEncoder.FallbackMessage;
                                }
                            }
                            catch (Exception exception)
                            {
                                codec = "MJPG";
                                LastRecordingWarning =
                                    "FFmpeg繧貞・譛溷喧縺ｧ縺阪↑縺・◆繧√∝ｾ捺擂縺ｮMJPEG縺ｸ蛻・ｊ譖ｿ縺医∪縺励◆: " +
                                    exception.Message;
                            }
                        }

                        string extension =
                            ffmpegEncoder is not null ? ".mp4" : ".avi";
                        string path = CreateRecordingPath(
                            options.Directory,
                            options.FileNamePrefix,
                            extension,
                            segmentNumber,
                            sessionTimestamp);
                        if (ffmpegEncoder is not null)
                        {
                            ffmpegWriter = new FfmpegVideoWriter(
                                path,
                                processed.Size(),
                                ffmpegEncoder);
                            LastRecordingEncoder =
                                ffmpegEncoder.DisplayName;
                        }
                        else
                        {
                            double fps = options.FramesPerSecond > 0
                                ? options.FramesPerSecond
                                : 30;
                            int fourCc = codec == "RAW"
                                ? 0
                                : VideoWriter.FourCC(
                                    codec[0],
                                    codec[1],
                                    codec[2],
                                    codec[3]);
                            writer = new VideoWriter(
                                path,
                                fourCc,
                                fps,
                                processed.Size());
                        if (!writer.IsOpened())
                        {
                            writer.Dispose();
                            writer = null;
                            throw new InvalidOperationException(
                                $"荳谺｡蜍慕判繝輔ぃ繧､繝ｫ繧剃ｽ懈・縺ｧ縺阪∪縺帙ｓ縺ｧ縺励◆: {path}");
                        }

                            LastRecordingEncoder = codec;
                        }

                        LastRecordingPath = path;
                        _lastRecordingPaths.Add(path);
                        segmentClock = Stopwatch.StartNew();
                    }

                    if (ffmpegWriter is not null)
                    {
                        ffmpegWriter.Write(processed);
                    }
                    else
                    {
                        writer!.Write(processed);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            LastRecordingError = exception;
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.RecordingLoopAsync: Recording failed",
                exception);
            ReportCaptureFault(exception);
        }
        finally
        {
            try
            {
                writer?.Release();
                writer?.Dispose();
                ffmpegWriter?.Dispose();
            }
            catch (Exception exception)
            {
                LastRecordingError ??= exception;
            }
        }
    }

    private static string NormalizeCodec(string codec)
    {
        string normalized = codec.Trim().ToUpperInvariant();
        return normalized is "RAW" or "MJPG" or "H264"
            ? normalized
            : "MJPG";
    }

    private static string CreateRecordingPath(
        string directory,
        string fileNamePrefix,
        string extension,
        int segmentNumber,
        string sessionTimestamp)
    {
        string prefix = string.IsNullOrWhiteSpace(fileNamePrefix)
            ? "capture"
            : fileNamePrefix;
        bool isRsBaseName = prefix.EndsWith(
            "~RSB",
            StringComparison.OrdinalIgnoreCase);
        string segmentPrefix = isRsBaseName
            ? IncrementRsBaseSerial(prefix, segmentNumber - 1)
            : prefix;
        string segment = !isRsBaseName && segmentNumber > 1
            ? $"_part{segmentNumber:D3}"
            : string.Empty;
        string fileName = prefix.EndsWith(
            "~RSB",
            StringComparison.OrdinalIgnoreCase)
                ? $"{segmentPrefix}{extension}"
                : $"{segmentPrefix}_{sessionTimestamp}{segment}{extension}";
        string candidate = Path.Combine(
            directory,
            fileName);
        int suffix = 1;
        while (File.Exists(candidate))
        {
            string conflictPrefix = isRsBaseName
                ? IncrementRsBaseSerial(
                    segmentPrefix,
                    suffix++)
                : segmentPrefix;
            string conflictFileName = prefix.EndsWith(
                "~RSB",
                StringComparison.OrdinalIgnoreCase)
                    ? $"{conflictPrefix}{extension}"
                    : $"{segmentPrefix}_{sessionTimestamp}{segment}_{suffix++:D2}{extension}";
            candidate = Path.Combine(
                directory,
                conflictFileName);
        }

        return candidate;
    }

    private static string IncrementRsBaseSerial(string fileStem, int increment)
    {
        if (increment <= 0)
        {
            return fileStem;
        }

        string[] parts = fileStem.Split('~');
        if (parts.Length < 5 ||
            !int.TryParse(parts[1], out int serial))
        {
            return fileStem;
        }

        parts[1] = Math.Clamp(serial + increment, 1, 9999)
            .ToString("D4");
        return string.Join("~", parts);
    }

    private Mat ApplyProcessing(Mat source)
    {
        ProcessingOptions options;
        lock (_optionsLock)
        {
            options = _options;
        }

        var result = source.Clone();
        if (options.FlipHorizontal || options.FlipVertical)
        {
            FlipMode flipMode =
                options.FlipHorizontal && options.FlipVertical
                    ? FlipMode.XY
                    : options.FlipHorizontal
                        ? FlipMode.Y
                        : FlipMode.X;
            Cv2.Flip(result, result, flipMode);
        }

        if (options.Red != 255 || options.Green != 255 || options.Blue != 255)
        {
            Mat[] channels = Cv2.Split(result);
            try
            {
                channels[0].ConvertTo(channels[0], MatType.CV_8U, options.Blue / 255.0);
                channels[1].ConvertTo(channels[1], MatType.CV_8U, options.Green / 255.0);
                channels[2].ConvertTo(channels[2], MatType.CV_8U, options.Red / 255.0);
                Cv2.Merge(channels, result);
            }
            finally
            {
                foreach (Mat channel in channels)
                {
                    channel.Dispose();
                }
            }
        }

        if (Math.Abs(options.Gamma - 1.0) > 0.001)
        {
            using var lookupTable = new Mat(1, 256, MatType.CV_8U);
            for (int value = 0; value < 256; value++)
            {
                double corrected = Math.Pow(value / 255.0, 1.0 / options.Gamma) * 255.0;
                lookupTable.Set(0, value, (byte)Math.Clamp((int)Math.Round(corrected), 0, 255));
            }

            Cv2.LUT(result, lookupTable, result);
        }

        return result;
    }

    private void ReportCaptureFault(Exception exception)
    {
        try
        {
            CaptureFaulted?.Invoke(exception);
        }
        catch (Exception callbackException)
        {
            ENTcapture2.Core.Services.DebugLogger.Error(
                "CameraCaptureService.ReportCaptureFault: Fault callback failed",
                callbackException);
        }
    }

    private void AddSnapshotCandidate(Mat frame, DateTime capturedAtUtc)
    {
        _snapshotCandidates.Enqueue(
            new SnapshotCandidate(frame.Clone(), capturedAtUtc));
        PruneSnapshotCandidates(
            capturedAtUtc.AddMilliseconds(-MaximumBestSnapshotWindowMilliseconds));
    }

    private void PruneSnapshotCandidates(DateTime cutoffUtc)
    {
        while (_snapshotCandidates.Count > 0 &&
               _snapshotCandidates.Peek().CapturedAtUtc < cutoffUtc)
        {
            _snapshotCandidates.Dequeue().Frame.Dispose();
        }
    }

    private void ClearSnapshotCandidates()
    {
        while (_snapshotCandidates.Count > 0)
        {
            _snapshotCandidates.Dequeue().Frame.Dispose();
        }
    }

    private static double CalculateSharpnessScore(
        Mat frame,
        string? roiText,
        out Rect scoringRoi)
    {
        scoringRoi = CenterCrop(ResolveSharpnessRoi(frame, roiText), 0.7);
        using Mat roiFrame = new(frame, scoringRoi);
        using Mat gray = new();
        if (roiFrame.Channels() == 1)
        {
            roiFrame.CopyTo(gray);
        }
        else
        {
            Cv2.CvtColor(roiFrame, gray, ColorConversionCodes.BGR2GRAY);
        }

        using Mat laplacian = new();
        Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
        Cv2.MeanStdDev(
            laplacian,
            out Scalar _,
            out Scalar standardDeviation);
        return standardDeviation.Val0 * standardDeviation.Val0;
    }

    private static void LogBestSnapshotSelection(
        int windowMilliseconds,
        string? roiText,
        Rect scoringRoi,
        IReadOnlyList<SnapshotCandidateScore> scores,
        BestSnapshotSelection selection)
    {
        double ratio = selection.LatestScore <= 0
            ? 1.0
            : selection.BestScore / selection.LatestScore;
        var lines = new List<string>
        {
            "BestSnapshot selection",
            $"  window={windowMilliseconds}ms",
            $"  configuredRoi=\"{roiText ?? string.Empty}\"",
            $"  scoringRoi={scoringRoi.X},{scoringRoi.Y},{scoringRoi.Width},{scoringRoi.Height}",
            $"  candidates={selection.CandidateCount}",
            $"  selectedAge={selection.SelectedAgeMilliseconds}ms",
            $"  usedBestFrame={selection.UsedBestFrame}",
            $"  bestScore={selection.BestScore:0.###}",
            $"  latestScore={selection.LatestScore:0.###}",
            $"  best/latest={ratio:0.###}"
        };

        foreach (SnapshotCandidateScore score in scores)
        {
            lines.Add(
                $"  frame#{score.Index:00} age={score.AgeMilliseconds,4}ms score={score.Score:0.###}");
        }

        ENTcapture2.Core.Services.DebugLogger.Info(string.Join(
            Environment.NewLine,
            lines));
    }

    private static Rect ResolveSharpnessRoi(Mat frame, string? roiText)
    {
        if (!string.IsNullOrWhiteSpace(roiText))
        {
            string[] parts = roiText.Split(
                ',',
                StringSplitOptions.TrimEntries);
            if (parts.Length == 4 &&
                int.TryParse(parts[0], out int x1) &&
                int.TryParse(parts[1], out int y1) &&
                int.TryParse(parts[2], out int x2) &&
                int.TryParse(parts[3], out int y2))
            {
                int left = Math.Clamp(Math.Min(x1, x2), 0, frame.Width - 1);
                int top = Math.Clamp(Math.Min(y1, y2), 0, frame.Height - 1);
                int right = Math.Clamp(Math.Max(x1, x2), left + 1, frame.Width);
                int bottom = Math.Clamp(Math.Max(y1, y2), top + 1, frame.Height);
                return new Rect(left, top, right - left, bottom - top);
            }
        }

        int width = Math.Max(1, (int)Math.Round(frame.Width * 0.7));
        int height = Math.Max(1, (int)Math.Round(frame.Height * 0.7));
        return new Rect(
            Math.Max(0, (frame.Width - width) / 2),
            Math.Max(0, (frame.Height - height) / 2),
            width,
            height);
    }

    private static Rect CenterCrop(Rect source, double ratio)
    {
        int width = Math.Max(1, (int)Math.Round(source.Width * ratio));
        int height = Math.Max(1, (int)Math.Round(source.Height * ratio));
        return new Rect(
            source.X + Math.Max(0, (source.Width - width) / 2),
            source.Y + Math.Max(0, (source.Height - height) / 2),
            width,
            height);
    }

    private sealed record ProcessingOptions(
        int Red,
        int Green,
        int Blue,
        double Gamma,
        bool FlipHorizontal,
        bool FlipVertical)
    {
        public static ProcessingOptions Default { get; } =
            new(255, 255, 255, 1.0, false, false);
    }

    private sealed record SnapshotCandidate(Mat Frame, DateTime CapturedAtUtc);

    private sealed record SnapshotCandidateScore(
        int Index,
        int AgeMilliseconds,
        double Score);
}

public sealed record CaptureStatistics(
    double InputFramesPerSecond,
    double PreviewFramesPerSecond,
    long DroppedFrames);

public sealed record BestSnapshotSelection(
    int CandidateCount,
    int SelectedAgeMilliseconds,
    double BestScore,
    double LatestScore,
    bool UsedBestFrame);

public sealed record RecordingOptions(
    string Directory,
    string Codec,
    double FramesPerSecond,
    string FileNamePrefix,
    int SegmentMinutes,
    string H264EncoderPreference);
