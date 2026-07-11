using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using CvSize = OpenCvSharp.Size;

namespace ENTcapture2.WinForms.Analysis;

internal sealed class GramStainAnalysisService
{
    public GramStainAnalysisResult Analyze(
        Mat source,
        GramStainRuleAnalysisOptions? options = null)
    {
        if (source.Empty())
        {
            throw new ArgumentException("画像が空です。", nameof(source));
        }

        options ??= GramStainRuleAnalysisOptions.Default;
        int minTargetPixels = Math.Clamp(options.MinTargetPixels, 1, 10000);
        int maxTargetPixels = Math.Clamp(
            Math.Max(options.MaxTargetPixels, minTargetPixels),
            minTargetPixels,
            10000);

        using Mat bgr = EnsureBgr(source);
        using Mat hsv = new();
        Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);

        using Mat saturationMask = new();
        using Mat brightMask = new();
        using Mat darkMask = new();
        using Mat candidateMask = new();
        Cv2.InRange(hsv, new Scalar(0, 35, 0), new Scalar(179, 255, 255), saturationMask);
        Cv2.InRange(hsv, new Scalar(0, 0, 35), new Scalar(179, 255, 245), brightMask);
        Cv2.BitwiseAnd(saturationMask, brightMask, candidateMask);
        Cv2.InRange(hsv, new Scalar(0, 0, 0), new Scalar(179, 255, 35), darkMask);
        Cv2.BitwiseOr(candidateMask, darkMask, candidateMask);

        using Mat kernel = Cv2.GetStructuringElement(
            MorphShapes.Ellipse,
            new CvSize(3, 3));
        Cv2.MorphologyEx(candidateMask, candidateMask, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(candidateMask, candidateMask, MorphTypes.Close, kernel);

        Cv2.FindContours(
            candidateMask,
            out CvPoint[][] contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        double minArea = Math.Max(3, minTargetPixels * minTargetPixels * 0.12);
        double maxArea = Math.Max(minArea + 1, maxTargetPixels * maxTargetPixels * 3.0);
        var detections = new List<GramStainDetection>();

        foreach (CvPoint[] contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area < minArea || area > maxArea)
            {
                continue;
            }

            Rect bounds = Cv2.BoundingRect(contour);
            int longSide = Math.Max(bounds.Width, bounds.Height);
            if (longSide < minTargetPixels || longSide > maxTargetPixels ||
                bounds.Width < 2 || bounds.Height < 2)
            {
                continue;
            }

            using Mat objectMask = Mat.Zeros(bgr.Rows, bgr.Cols, MatType.CV_8UC1);
            Cv2.DrawContours(objectMask, [contour], -1, Scalar.White, -1);
            Scalar meanBgr = Cv2.Mean(bgr, objectMask);
            double perimeter = Math.Max(1.0, Cv2.ArcLength(contour, true));
            double circularity = 4.0 * Math.PI * area / (perimeter * perimeter);
            double aspectRatio = GetAspectRatio(contour, bounds);
            GramStainPolarity gram = ClassifyGram(meanBgr);
            BacteriumShape shape = ClassifyShape(aspectRatio, circularity);
            double confidence = EstimateConfidence(
                gram,
                shape,
                aspectRatio,
                circularity,
                area,
                minArea);

            detections.Add(
                new GramStainDetection(
                    bounds,
                    contour,
                    gram,
                    shape,
                    confidence,
                    area,
                    aspectRatio,
                    circularity,
                    meanBgr));
        }

        Mat overlay = bgr.Clone();
        DrawOverlay(overlay, detections);

        return new GramStainAnalysisResult(
            detections,
            CreateCounts(detections),
            CreateCandidateSummary(detections),
            overlay);
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

    private static double GetAspectRatio(CvPoint[] contour, Rect bounds)
    {
        if (contour.Length >= 5)
        {
            RotatedRect box = Cv2.MinAreaRect(contour);
            double width = Math.Max(1.0, box.Size.Width);
            double height = Math.Max(1.0, box.Size.Height);
            return Math.Max(width, height) / Math.Min(width, height);
        }

        double rectWidth = Math.Max(1.0, bounds.Width);
        double rectHeight = Math.Max(1.0, bounds.Height);
        return Math.Max(rectWidth, rectHeight) / Math.Min(rectWidth, rectHeight);
    }

    private static GramStainPolarity ClassifyGram(Scalar meanBgr)
    {
        double blue = meanBgr.Val0;
        double green = meanBgr.Val1;
        double red = meanBgr.Val2;
        bool purpleLike =
            blue > green * 1.05 &&
            red > green * 1.02 &&
            (blue + red) * 0.5 > 65;
        bool pinkLike =
            red > blue * 1.08 &&
            red > green * 0.92 &&
            red > 70;

        if (purpleLike && !pinkLike)
        {
            return GramStainPolarity.Positive;
        }

        if (pinkLike && !purpleLike)
        {
            return GramStainPolarity.Negative;
        }

        if (purpleLike)
        {
            return GramStainPolarity.Positive;
        }

        if (pinkLike)
        {
            return GramStainPolarity.Negative;
        }

        return GramStainPolarity.Uncertain;
    }

    private static BacteriumShape ClassifyShape(
        double aspectRatio,
        double circularity)
    {
        if (aspectRatio >= 2.1)
        {
            return BacteriumShape.Bacillus;
        }

        if (aspectRatio <= 1.75 && circularity >= 0.45)
        {
            return BacteriumShape.Coccus;
        }

        return BacteriumShape.Uncertain;
    }

    private static double EstimateConfidence(
        GramStainPolarity gram,
        BacteriumShape shape,
        double aspectRatio,
        double circularity,
        double area,
        double minArea)
    {
        double score = 0.45;
        if (gram != GramStainPolarity.Uncertain)
        {
            score += 0.2;
        }

        if (shape == BacteriumShape.Bacillus)
        {
            score += Math.Clamp((aspectRatio - 2.0) / 3.0, 0, 0.2);
        }
        else if (shape == BacteriumShape.Coccus)
        {
            score += Math.Clamp(circularity - 0.45, 0, 0.2);
        }

        score += Math.Clamp((area - minArea) / Math.Max(1, minArea * 6), 0, 0.15);
        return Math.Clamp(score, 0.05, 0.98);
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
            "ルール解析: 色と輪郭サイズによる簡易判定です。最終判断には使わず参考値として確認してください。"
        };
        if (counts.GramPositiveCocci > 0)
        {
            candidates.Add("G+球菌: ブドウ球菌/レンサ球菌/腸球菌などの候補。配列確認が必要です。");
        }

        if (counts.GramNegativeCocci > 0)
        {
            candidates.Add("G-球菌: Neisseria/Moraxella などの候補。双球菌か要確認です。");
        }

        if (counts.GramPositiveBacilli > 0)
        {
            candidates.Add("G+桿菌: Corynebacterium/Bacillus/Clostridium などの候補。形態と背景を要確認です。");
        }

        if (counts.GramNegativeBacilli > 0)
        {
            candidates.Add("G-桿菌: Enterobacterales/Pseudomonas などの候補。菌名推定は培養等が必要です。");
        }

        if (counts.Uncertain > 0)
        {
            candidates.Add($"不明/要確認: {counts.Uncertain} 個。重なり、染色ムラ、ゴミの可能性があります。");
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
            Cv2.Rectangle(image, detection.Bounds, color, 3);
            Cv2.DrawContours(image, [detection.Contour], -1, color, 2);
            Cv2.PutText(
                image,
                detection.ShortLabel,
                new CvPoint(detection.Bounds.Left, Math.Max(12, detection.Bounds.Top - 3)),
                HersheyFonts.HersheySimplex,
                0.46,
                color,
                2,
                LineTypes.AntiAlias);
        }
    }

    private static Scalar GetOverlayColor(GramStainDetection detection)
    {
        return (detection.Gram, detection.Shape) switch
        {
            (GramStainPolarity.Positive, BacteriumShape.Coccus) =>
                new Scalar(255, 220, 0),
            (GramStainPolarity.Negative, BacteriumShape.Coccus) =>
                new Scalar(255, 150, 0),
            (GramStainPolarity.Positive, BacteriumShape.Bacillus) =>
                new Scalar(120, 255, 0),
            (GramStainPolarity.Negative, BacteriumShape.Bacillus) =>
                new Scalar(255, 255, 0),
            _ => new Scalar(0, 255, 180)
        };
    }
}

internal sealed record GramStainRuleAnalysisOptions(
    int MinTargetPixels,
    int MaxTargetPixels)
{
    public static GramStainRuleAnalysisOptions Default { get; } = new(10, 100);
}

internal sealed record GramStainAnalysisResult(
    IReadOnlyList<GramStainDetection> Detections,
    GramStainCounts Counts,
    IReadOnlyList<string> CandidateSummary,
    Mat OverlayImage) : IDisposable
{
    public void Dispose()
    {
        OverlayImage.Dispose();
    }
}

internal sealed record GramStainDetection(
    Rect Bounds,
    CvPoint[] Contour,
    GramStainPolarity Gram,
    BacteriumShape Shape,
    double Confidence,
    double Area,
    double AspectRatio,
    double Circularity,
    Scalar MeanBgr,
    int ClassId = -1,
    string? ClassName = null)
{
    public string ShortLabel =>
        !string.IsNullOrWhiteSpace(ClassName)
            ? ClassName
            : $"{(Gram == GramStainPolarity.Positive ? "G+" : Gram == GramStainPolarity.Negative ? "G-" : "G?")}" +
        $"{(Shape == BacteriumShape.Coccus ? "球" : Shape == BacteriumShape.Bacillus ? "桿" : "?")}";

    public string DisplayClass =>
        !string.IsNullOrWhiteSpace(ClassName)
            ? ClassName
            : $"{ToDisplayName(Gram)} {ToDisplayName(Shape)}";

    private static string ToDisplayName(GramStainPolarity gram) =>
        gram switch
        {
            GramStainPolarity.Positive => "グラム陽性",
            GramStainPolarity.Negative => "グラム陰性",
            _ => "Gram不明"
        };

    private static string ToDisplayName(BacteriumShape shape) =>
        shape switch
        {
            BacteriumShape.Coccus => "球菌",
            BacteriumShape.Bacillus => "桿菌",
            _ => "形状不明"
        };
}

internal sealed record GramStainCounts(
    int GramPositiveCocci,
    int GramNegativeCocci,
    int GramPositiveBacilli,
    int GramNegativeBacilli,
    int Uncertain)
{
    public int Total =>
        GramPositiveCocci +
        GramNegativeCocci +
        GramPositiveBacilli +
        GramNegativeBacilli +
        Uncertain;
}

internal enum GramStainPolarity
{
    Positive,
    Negative,
    Uncertain
}

internal enum BacteriumShape
{
    Coccus,
    Bacillus,
    Uncertain
}
