using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using CvSize = OpenCvSharp.Size;

namespace ENTcapture2.WinForms.Analysis;

internal sealed class YoloBacteriaDetector : IDisposable
{
    private const int DefaultInputSize = 640;
    private readonly InferenceSession _session;
    private readonly IReadOnlyList<YoloModelClass> _classes;
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
        _classes = LoadModelClasses(modelPath);
    }

    public IReadOnlyList<YoloModelClass> Classes => _classes;

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
        List<YoloCandidate> candidates = DetectCandidates(
            bgr,
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

    public GramStainAnalysisResult AnalyzeTiled(
        Mat source,
        float confidenceThreshold,
        float iouThreshold,
        int tileSize = DefaultInputSize,
        int stride = 512)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (source.Empty())
        {
            throw new ArgumentException("画像が空です。", nameof(source));
        }

        using Mat bgr = EnsureBgr(source);
        int effectiveTileWidth = Math.Min(Math.Max(1, tileSize), bgr.Width);
        int effectiveTileHeight = Math.Min(Math.Max(1, tileSize), bgr.Height);
        var candidates = new List<YoloCandidate>();
        foreach (int y in CreateTilePositions(bgr.Height, effectiveTileHeight, stride))
        {
            foreach (int x in CreateTilePositions(bgr.Width, effectiveTileWidth, stride))
            {
                using Mat roi = new(
                    bgr,
                    new Rect(x, y, effectiveTileWidth, effectiveTileHeight));
                candidates.AddRange(
                    DetectCandidates(roi, Math.Clamp(confidenceThreshold, 0.01F, 0.99F))
                        .Select(candidate => OffsetCandidate(candidate, x, y, bgr.Size())));
            }
        }

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
            CreateTiledCandidateSummary(detections, candidates.Count),
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

    private static IReadOnlyList<YoloModelClass> LoadModelClasses(string modelPath)
    {
        string? yamlPath = FindYamlPath(modelPath);
        if (yamlPath is not null)
        {
            IReadOnlyList<YoloModelClass> classes = ParseYoloNames(File.ReadAllLines(yamlPath));
            if (classes.Count > 0)
            {
                return classes;
            }
        }

        return
        [
            new(0, "G-cocci"),
            new(1, "G+cocci"),
            new(2, "G-bacilli"),
            new(3, "G+bacilli")
        ];
    }

    private static string? FindYamlPath(string modelPath)
    {
        string? directory = Path.GetDirectoryName(modelPath);
        string baseName = Path.GetFileNameWithoutExtension(modelPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        string[] candidates =
        [
            Path.Combine(directory, baseName + ".yaml"),
            Path.Combine(directory, baseName + ".yml"),
            Path.Combine(directory, "data.yaml"),
            Path.Combine(directory, "data.yml")
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    private static IReadOnlyList<YoloModelClass> ParseYoloNames(IEnumerable<string> lines)
    {
        var classes = new SortedDictionary<int, string>();
        bool inNames = false;
        foreach (string rawLine in lines)
        {
            string line = StripYamlComment(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string trimmed = line.Trim();
            if (!inNames)
            {
                if (trimmed == "names:")
                {
                    inNames = true;
                    continue;
                }

                if (trimmed.StartsWith("names:", StringComparison.Ordinal))
                {
                    ParseInlineNames(trimmed["names:".Length..].Trim(), classes);
                    break;
                }

                continue;
            }

            if (!char.IsWhiteSpace(rawLine[0]))
            {
                break;
            }

            int separator = trimmed.IndexOf(':');
            if (separator > 0 &&
                int.TryParse(trimmed[..separator].Trim(), out int id))
            {
                classes[id] = UnquoteYamlValue(trimmed[(separator + 1)..].Trim());
                continue;
            }

            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                classes[classes.Count] = UnquoteYamlValue(trimmed[1..].Trim());
            }
        }

        return classes
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => new YoloModelClass(item.Key, item.Value))
            .ToList();
    }

    private static void ParseInlineNames(
        string value,
        IDictionary<int, string> classes)
    {
        if (!value.StartsWith("[", StringComparison.Ordinal) ||
            !value.EndsWith("]", StringComparison.Ordinal))
        {
            return;
        }

        string inner = value[1..^1];
        foreach (string item in inner.Split(','))
        {
            classes[classes.Count] = UnquoteYamlValue(item.Trim());
        }
    }

    private static string StripYamlComment(string line)
    {
        int index = line.IndexOf('#');
        return index >= 0 ? line[..index] : line;
    }

    private static string UnquoteYamlValue(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '"' && trimmed[^1] == '"') ||
             (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static (GramStainPolarity Gram, BacteriumShape Shape) InferBacteriaClass(string className)
    {
        string normalized = className
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .ToLowerInvariant();
        GramStainPolarity gram = normalized.Contains("g+", StringComparison.Ordinal) ||
            normalized.Contains("positive", StringComparison.Ordinal)
            ? GramStainPolarity.Positive
            : normalized.Contains("g-", StringComparison.Ordinal) ||
              normalized.Contains("negative", StringComparison.Ordinal)
            ? GramStainPolarity.Negative
            : GramStainPolarity.Uncertain;
        BacteriumShape shape = normalized.Contains("cocci", StringComparison.Ordinal) ||
            normalized.Contains("coccus", StringComparison.Ordinal)
            ? BacteriumShape.Coccus
            : normalized.Contains("bacilli", StringComparison.Ordinal) ||
              normalized.Contains("bacillus", StringComparison.Ordinal)
            ? BacteriumShape.Bacillus
            : BacteriumShape.Uncertain;
        return (gram, shape);
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

    private List<YoloCandidate> DetectCandidates(
        Mat bgr,
        float confidenceThreshold)
    {
        LetterboxImage letterbox = CreateLetterbox(bgr);
        using Mat inputImage = letterbox.Image;
        DenseTensor<float> tensor = CreateInputTensor(inputImage);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs =
            _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, tensor)]);
        Tensor<float> output = outputs.First().AsTensor<float>();
        return ParseOutput(
            output,
            letterbox,
            bgr.Size(),
            confidenceThreshold);
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

    private List<YoloCandidate> ParseOutput(
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
                if (!TryMapClass(
                    classIndex,
                    out GramStainPolarity gram,
                    out BacteriumShape shape,
                    out string className))
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
                candidates.Add(new YoloCandidate(bounds, classIndex, className, gram, shape, score));
                continue;
            }

            if (attributes < 4 + _classes.Count)
            {
                continue;
            }

            float objectness = attributes > 4 + _classes.Count
                ? GetValue(output, channelFirst, 4, row)
                : 1F;
            int classOffset = attributes > 4 + _classes.Count ? 5 : 4;
            float bestScore = 0;
            int bestClass = -1;
            for (int classIndex = 0; classIndex < _classes.Count; classIndex++)
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
                !TryMapClass(
                    bestClass,
                    out GramStainPolarity gramValue,
                    out BacteriumShape shapeValue,
                    out string classValue))
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
            candidates.Add(new YoloCandidate(box, bestClass, classValue, gramValue, shapeValue, bestScore));
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

    private bool TryMapClass(
        int classIndex,
        out GramStainPolarity gram,
        out BacteriumShape shape,
        out string className)
    {
        gram = GramStainPolarity.Uncertain;
        shape = BacteriumShape.Uncertain;
        className = string.Empty;
        if (classIndex < 0 || classIndex >= _classes.Count)
        {
            return false;
        }

        className = _classes[classIndex].Name;
        (gram, shape) = InferBacteriaClass(className);
        return true;
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
                item.ClassId == candidate.ClassId &&
                CalculateIou(item.Bounds, candidate.Bounds) > iouThreshold);
            if (!overlaps)
            {
                selected.Add(candidate);
            }
        }

        return selected;
    }

    private static IEnumerable<int> CreateTilePositions(int fullSize, int tileSize, int stride)
    {
        if (fullSize <= tileSize)
        {
            yield return 0;
            yield break;
        }

        var positions = new List<int>();
        int step = Math.Clamp(stride, 1, tileSize);
        for (int position = 0; position <= fullSize - tileSize; position += step)
        {
            positions.Add(position);
        }

        int last = fullSize - tileSize;
        if (positions.Count == 0 || positions[^1] != last)
        {
            positions.Add(last);
        }

        foreach (int position in positions)
        {
            yield return position;
        }
    }

    private static YoloCandidate OffsetCandidate(
        YoloCandidate candidate,
        int offsetX,
        int offsetY,
        CvSize fullSize)
    {
        int left = Math.Clamp(candidate.Bounds.Left + offsetX, 0, fullSize.Width - 1);
        int top = Math.Clamp(candidate.Bounds.Top + offsetY, 0, fullSize.Height - 1);
        int right = Math.Clamp(candidate.Bounds.Right + offsetX, left + 1, fullSize.Width);
        int bottom = Math.Clamp(candidate.Bounds.Bottom + offsetY, top + 1, fullSize.Height);
        return candidate with
        {
            Bounds = new Rect(left, top, right - left, bottom - top)
        };
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
            Scalar.All(0),
            candidate.ClassId,
            candidate.ClassName);
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
        var candidates = new List<string>
        {
            "AI解析: ONNXと同じフォルダのyamlからクラス名を読み込んでいます。"
        };
        candidates.AddRange(
            detections
                .GroupBy(item => item.DisplayClass)
                .OrderByDescending(group => group.Count())
                .Take(12)
                .Select(group => $"{group.Key}: {group.Count()} 個"));

        return candidates;
    }

    private static IReadOnlyList<string> CreateTiledCandidateSummary(
        IReadOnlyList<GramStainDetection> detections,
        int rawCandidateCount)
    {
        List<string> candidates = [.. CreateCandidateSummary(detections)];
        candidates.Insert(
            1,
            $"tile解析: 640px tile / stride 512pxで推論し、重なり領域はNMSで統合しました。統合前候補 {rawCandidateCount} 個。");
        return candidates;
    }

    private static void DrawOverlay(
        Mat image,
        IReadOnlyList<GramStainDetection> detections)
    {
        foreach (GramStainDetection detection in detections)
        {
            Scalar color = GetOverlayColor(detection);
            Cv2.Rectangle(image, detection.Bounds, color, 3);
            string label = $"{detection.ShortLabel} {detection.Confidence:0.00}";
            Cv2.PutText(
                image,
                label,
                new CvPoint(detection.Bounds.Left, Math.Max(12, detection.Bounds.Top - 3)),
                HersheyFonts.HersheySimplex,
                0.48,
                color,
                2,
                LineTypes.AntiAlias);
        }
    }

    private static Scalar GetOverlayColor(GramStainDetection detection)
    {
        return ToPaletteColor(detection.ClassId);
    }

    private static Scalar ToPaletteColor(int classId)
    {
        Scalar[] palette =
        [
            new(255, 220, 0),
            new(255, 150, 0),
            new(120, 255, 0),
            new(255, 255, 0),
            new(0, 255, 180),
            new(255, 80, 200),
            new(80, 160, 255),
            new(180, 255, 80)
        ];
        return palette[Math.Abs(classId) % palette.Length];
    }

    private sealed record LetterboxImage(
        Mat Image,
        double Scale,
        int PadX,
        int PadY);

    private sealed record YoloCandidate(
        Rect Bounds,
        int ClassId,
        string ClassName,
        GramStainPolarity Gram,
        BacteriumShape Shape,
        float Score);
}

internal sealed record YoloModelClass(
    int Id,
    string Name);
