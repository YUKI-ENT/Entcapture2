using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using CvSize = OpenCvSharp.Size;

namespace ENTcapture2.WinForms.Analysis;

internal sealed class YoloBacteriaDetector : IDisposable
{
    private const int DefaultInputSize = 640;
    private const int ClassCount = 4;
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _inputWidth;
    private readonly int _inputHeight;
    private bool _disposed;

    public YoloBacteriaDetector(string modelPath)
    {
        _session = new InferenceSession(modelPath);
        _inputName = _session.InputMetadata.Keys.First();
        NodeMetadata input = _session.InputMetadata[_inputName];
        int[] dimensions = input.Dimensions;
        _inputHeight = GetDimension(dimensions, 2, DefaultInputSize);
        _inputWidth = GetDimension(dimensions, 3, DefaultInputSize);
    }

    public GramStainAnalysisResult Analyze(
        Mat source,
        float confidenceThreshold,
        float iouThreshold)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (source.Empty())
        {
            throw new ArgumentException("画像が空です。", nameof(source));
        }

        using Mat bgr = EnsureBgr(source);
        LetterboxImage letterbox = CreateLetterbox(bgr);
        using Mat inputImage = letterbox.Image;
        DenseTensor<float> tensor = CreateInputTensor(inputImage);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs =
            _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, tensor)]);
        Tensor<float> output = outputs.First().AsTensor<float>();
        List<YoloCandidate> candidates = ParseOutput(
            output,
            letterbox,
            bgr.Size(),
            Math.Clamp(confidenceThreshold, 0.01F, 0.99F));
        List<YoloCandidate> selected = ApplyNms(
            candidates,
            Math.Clamp(iouThreshold, 0.01F, 0.99F));
        List<GramStainDetection> detections = selected
            .Select(ToDetection)
            .ToList();
        Mat overlay = bgr.Clone();
        DrawOverlay(overlay, detections);

        return new GramStainAnalysisResult(
            detections,
            CreateCounts(detections),
            CreateCandidateSummary(detections),
            overlay);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Dispose();
    }

    private static int GetDimension(
        IReadOnlyList<int> dimensions,
        int index,
        int fallback)
    {
        if (dimensions.Count <= index || dimensions[index] <= 0)
        {
            return fallback;
        }

        return dimensions[index];
    }

    private static Mat EnsureBgr(Mat source)
    {
        if (source.Channels() == 3)
        {
            return source.Clone();
        }

        var bgr = new Mat();
        if (source.Channels() == 4)
        {
            Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
        }
        else
        {
            Cv2.CvtColor(source, bgr, ColorConversionCodes.GRAY2BGR);
        }

        return bgr;
    }

    private LetterboxImage CreateLetterbox(Mat source)
    {
        double scale = Math.Min(
            _inputWidth / (double)source.Width,
            _inputHeight / (double)source.Height);
        int resizedWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        int resizedHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        int padX = (_inputWidth - resizedWidth) / 2;
        int padY = (_inputHeight - resizedHeight) / 2;
        var canvas = new Mat(
            _inputHeight,
            _inputWidth,
            MatType.CV_8UC3,
            new Scalar(114, 114, 114));
        using var resized = new Mat();
        Cv2.Resize(source, resized, new CvSize(resizedWidth, resizedHeight));
        using Mat roi = new(
            canvas,
            new Rect(padX, padY, resizedWidth, resizedHeight));
        resized.CopyTo(roi);
        return new LetterboxImage(canvas, scale, padX, padY);
    }

    private DenseTensor<float> CreateInputTensor(Mat bgr)
    {
        using var rgb = new Mat();
        Cv2.CvtColor(bgr, rgb, ColorConversionCodes.BGR2RGB);
        var tensor = new DenseTensor<float>(
            [1, 3, _inputHeight, _inputWidth]);
        for (int y = 0; y < _inputHeight; y++)
        {
            for (int x = 0; x < _inputWidth; x++)
            {
                Vec3b pixel = rgb.At<Vec3b>(y, x);
                tensor[0, 0, y, x] = pixel.Item0 / 255F;
                tensor[0, 1, y, x] = pixel.Item1 / 255F;
                tensor[0, 2, y, x] = pixel.Item2 / 255F;
            }
        }

        return tensor;
    }

    private static List<YoloCandidate> ParseOutput(
        Tensor<float> output,
        LetterboxImage letterbox,
        CvSize originalSize,
        float confidenceThreshold)
    {
        int[] dims = output.Dimensions.ToArray();
        if (dims.Length < 3)
        {
            throw new InvalidOperationException(
                $"YOLO出力形状に対応していません: [{string.Join(",", dims)}]");
        }

        int dim1 = dims[^2];
        int dim2 = dims[^1];
        bool channelFirst = dim1 <= dim2 && dim1 >= 6;
        int rows = channelFirst ? dim2 : dim1;
        int attributes = channelFirst ? dim1 : dim2;
        var candidates = new List<YoloCandidate>();

        for (int row = 0; row < rows; row++)
        {
            if (attributes == 6)
            {
                float score = GetValue(output, channelFirst, 4, row);
                if (score < confidenceThreshold)
                {
                    continue;
                }

                int classIndex = (int)Math.Round(
                    GetValue(output, channelFirst, 5, row));
                if (!TryMapClass(classIndex, out GramStainPolarity gram, out BacteriumShape shape))
                {
                    continue;
                }

                Rect bounds = ToOriginalRect(
                    GetValue(output, channelFirst, 0, row),
                    GetValue(output, channelFirst, 1, row),
                    GetValue(output, channelFirst, 2, row),
                    GetValue(output, channelFirst, 3, row),
                    coordinatesAreCorners: true,
                    letterbox,
                    originalSize);
                candidates.Add(new YoloCandidate(bounds, gram, shape, score));
                continue;
            }

            if (attributes < 4 + ClassCount)
            {
                continue;
            }

            float objectness = attributes > 4 + ClassCount
                ? GetValue(output, channelFirst, 4, row)
                : 1F;
            int classOffset = attributes > 4 + ClassCount ? 5 : 4;
            float bestScore = 0;
            int bestClass = -1;
            for (int classIndex = 0; classIndex < ClassCount; classIndex++)
            {
                float classScore =
                    objectness * GetValue(output, channelFirst, classOffset + classIndex, row);
                if (classScore > bestScore)
                {
                    bestScore = classScore;
                    bestClass = classIndex;
                }
            }

            if (bestScore < confidenceThreshold ||
                !TryMapClass(bestClass, out GramStainPolarity gramValue, out BacteriumShape shapeValue))
            {
                continue;
            }

            Rect box = ToOriginalRect(
                GetValue(output, channelFirst, 0, row),
                GetValue(output, channelFirst, 1, row),
                GetValue(output, channelFirst, 2, row),
                GetValue(output, channelFirst, 3, row),
                coordinatesAreCorners: false,
                letterbox,
                originalSize);
            candidates.Add(new YoloCandidate(box, gramValue, shapeValue, bestScore));
        }

        return candidates;
    }

    private static float GetValue(
        Tensor<float> output,
        bool channelFirst,
        int attribute,
        int row)
    {
        return channelFirst
            ? output[0, attribute, row]
            : output[0, row, attribute];
    }

    private static bool TryMapClass(
        int classIndex,
        out GramStainPolarity gram,
        out BacteriumShape shape)
    {
        (gram, shape) = classIndex switch
        {
            0 => (GramStainPolarity.Negative, BacteriumShape.Coccus),
            1 => (GramStainPolarity.Positive, BacteriumShape.Coccus),
            2 => (GramStainPolarity.Negative, BacteriumShape.Bacillus),
            3 => (GramStainPolarity.Positive, BacteriumShape.Bacillus),
            _ => (GramStainPolarity.Uncertain, BacteriumShape.Uncertain)
        };
        return classIndex is >= 0 and < ClassCount;
    }

    private static Rect ToOriginalRect(
        float x1,
        float y1,
        float x2OrWidth,
        float y2OrHeight,
        bool coordinatesAreCorners,
        LetterboxImage letterbox,
        CvSize originalSize)
    {
        double left;
        double top;
        double right;
        double bottom;
        if (coordinatesAreCorners)
        {
            left = x1;
            top = y1;
            right = x2OrWidth;
            bottom = y2OrHeight;
        }
        else
        {
            left = x1 - x2OrWidth / 2.0;
            top = y1 - y2OrHeight / 2.0;
            right = x1 + x2OrWidth / 2.0;
            bottom = y1 + y2OrHeight / 2.0;
        }

        left = (left - letterbox.PadX) / letterbox.Scale;
        top = (top - letterbox.PadY) / letterbox.Scale;
        right = (right - letterbox.PadX) / letterbox.Scale;
        bottom = (bottom - letterbox.PadY) / letterbox.Scale;

        int clampedLeft = Math.Clamp((int)Math.Round(left), 0, originalSize.Width - 1);
        int clampedTop = Math.Clamp((int)Math.Round(top), 0, originalSize.Height - 1);
        int clampedRight = Math.Clamp((int)Math.Round(right), clampedLeft + 1, originalSize.Width);
        int clampedBottom = Math.Clamp((int)Math.Round(bottom), clampedTop + 1, originalSize.Height);
        return new Rect(
            clampedLeft,
            clampedTop,
            clampedRight - clampedLeft,
            clampedBottom - clampedTop);
    }

    private static List<YoloCandidate> ApplyNms(
        IEnumerable<YoloCandidate> candidates,
        float iouThreshold)
    {
        var selected = new List<YoloCandidate>();
        foreach (YoloCandidate candidate in candidates.OrderByDescending(item => item.Score))
        {
            bool overlaps = selected.Any(item =>
                item.Gram == candidate.Gram &&
                item.Shape == candidate.Shape &&
                CalculateIou(item.Bounds, candidate.Bounds) > iouThreshold);
            if (!overlaps)
            {
                selected.Add(candidate);
            }
        }

        return selected;
    }

    private static double CalculateIou(Rect a, Rect b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        int width = Math.Max(0, right - left);
        int height = Math.Max(0, bottom - top);
        double intersection = width * height;
        double union = a.Width * a.Height + b.Width * b.Height - intersection;
        return union <= 0 ? 0 : intersection / union;
    }

    private static GramStainDetection ToDetection(YoloCandidate candidate)
    {
        CvPoint[] contour =
        [
            new(candidate.Bounds.Left, candidate.Bounds.Top),
            new(candidate.Bounds.Right, candidate.Bounds.Top),
            new(candidate.Bounds.Right, candidate.Bounds.Bottom),
            new(candidate.Bounds.Left, candidate.Bounds.Bottom)
        ];
        double area = candidate.Bounds.Width * candidate.Bounds.Height;
        double aspectRatio = Math.Max(candidate.Bounds.Width, candidate.Bounds.Height) /
            (double)Math.Max(1, Math.Min(candidate.Bounds.Width, candidate.Bounds.Height));
        return new GramStainDetection(
            candidate.Bounds,
            contour,
            candidate.Gram,
            candidate.Shape,
            candidate.Score,
            area,
            aspectRatio,
            0,
            Scalar.All(0));
    }

    private static GramStainCounts CreateCounts(
        IReadOnlyList<GramStainDetection> detections)
    {
        return new GramStainCounts(
            detections.Count(item =>
                item.Gram == GramStainPolarity.Positive &&
                item.Shape == BacteriumShape.Coccus),
            detections.Count(item =>
                item.Gram == GramStainPolarity.Negative &&
                item.Shape == BacteriumShape.Coccus),
            detections.Count(item =>
                item.Gram == GramStainPolarity.Positive &&
                item.Shape == BacteriumShape.Bacillus),
            detections.Count(item =>
                item.Gram == GramStainPolarity.Negative &&
                item.Shape == BacteriumShape.Bacillus),
            detections.Count(item =>
                item.Gram == GramStainPolarity.Uncertain ||
                item.Shape == BacteriumShape.Uncertain));
    }

    private static IReadOnlyList<string> CreateCandidateSummary(
        IReadOnlyList<GramStainDetection> detections)
    {
        GramStainCounts counts = CreateCounts(detections);
        var candidates = new List<string>
        {
            "AI解析: Clinical Bacteria DataSet形式のYOLO ONNXを想定しています。",
            "クラス順: G-cocci, G+cocci, G-bacilli, G+bacilli。学習時の順序が異なる場合は結果が入れ替わります。"
        };
        if (counts.GramPositiveCocci > 0)
        {
            candidates.Add("G+球菌: Staphylococcus/Streptococcus/Enterococcus などの候補。");
        }

        if (counts.GramNegativeCocci > 0)
        {
            candidates.Add("G-球菌: Neisseria/Moraxella などの候補。");
        }

        if (counts.GramPositiveBacilli > 0)
        {
            candidates.Add("G+桿菌: Corynebacterium/Bacillus/Clostridium などの候補。");
        }

        if (counts.GramNegativeBacilli > 0)
        {
            candidates.Add("G-桿菌: Enterobacterales/Pseudomonas などの候補。");
        }

        return candidates;
    }

    private static void DrawOverlay(
        Mat image,
        IReadOnlyList<GramStainDetection> detections)
    {
        foreach (GramStainDetection detection in detections)
        {
            Scalar color = GetOverlayColor(detection);
            Cv2.Rectangle(image, detection.Bounds, color, 2);
            string label = $"{detection.ShortLabel} {detection.Confidence:0.00}";
            Cv2.PutText(
                image,
                label,
                new CvPoint(detection.Bounds.Left, Math.Max(12, detection.Bounds.Top - 3)),
                HersheyFonts.HersheySimplex,
                0.42,
                color,
                1,
                LineTypes.AntiAlias);
        }
    }

    private static Scalar GetOverlayColor(GramStainDetection detection)
    {
        return (detection.Gram, detection.Shape) switch
        {
            (GramStainPolarity.Positive, BacteriumShape.Coccus) =>
                new Scalar(220, 90, 210),
            (GramStainPolarity.Negative, BacteriumShape.Coccus) =>
                new Scalar(90, 120, 255),
            (GramStainPolarity.Positive, BacteriumShape.Bacillus) =>
                new Scalar(255, 70, 160),
            (GramStainPolarity.Negative, BacteriumShape.Bacillus) =>
                new Scalar(50, 190, 255),
            _ => new Scalar(120, 220, 220)
        };
    }

    private sealed record LetterboxImage(
        Mat Image,
        double Scale,
        int PadX,
        int PadY);

    private sealed record YoloCandidate(
        Rect Bounds,
        GramStainPolarity Gram,
        BacteriumShape Shape,
        float Score);
}
