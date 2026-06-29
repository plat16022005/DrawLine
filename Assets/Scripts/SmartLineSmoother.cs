using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility tĩnh nhận dạng hình dạng người chơi muốn vẽ và làm mịn nét vẽ tương ứng.
/// Được gọi một lần duy nhất khi người chơi nhấc tay (MouseUp), không ảnh hưởng hiệu năng.
/// </summary>
public static class SmartLineSmoother
{
    // ─── Tham số (được gán từ LineCreator qua Inspector) ─────────────────────
    /// <summary>Ngưỡng sai số để coi là đường thẳng (0 = thẳng tuyệt đối, 1 = rất cong)</summary>
    public static float StraightLineThreshold = 0.08f;
    /// <summary>Tỉ lệ width/height (hoặc ngược lại) để snap vào trục ngang/dọc</summary>
    public static float AxisSnapRatio = 3.0f;
    /// <summary>Số điểm output khi resample đường cong Catmull-Rom</summary>
    public static int CurveResolution = 40;
    /// <summary>Số điểm control khi downsample trước khi fit spline</summary>
    public static int CurveControlPoints = 8;
    /// <summary>Số điểm tối thiểu trong nét vẽ mới xử lý smart draw</summary>
    public static int MinPointsToSmooth = 5;

    // ─── Enum loại hình dạng ─────────────────────────────────────────────────
    public enum ShapeType { Horizontal, Vertical, Diagonal, Curve }

    // ─────────────────────────────────────────────────────────────────────────
    // API CHÍNH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Nhận dạng hình dạng từ danh sách điểm thô.
    /// </summary>
    public static ShapeType Recognize(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 2) return ShapeType.Diagonal;

        // Bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float width  = maxX - minX;
        float height = maxY - minY;
        float linearityError = ComputeLinearityError(points);

        if (linearityError < StraightLineThreshold)
        {
            // Đường đủ thẳng — phân loại theo tỉ lệ bounding box
            bool dominantlyHorizontal = (height < 0.0001f) ||
                                        (width > 0.0001f && width / height > AxisSnapRatio);
            bool dominantlyVertical   = (width  < 0.0001f) ||
                                        (height > 0.0001f && height / width > AxisSnapRatio);

            if (dominantlyHorizontal) return ShapeType.Horizontal;
            if (dominantlyVertical)   return ShapeType.Vertical;
            return ShapeType.Diagonal;
        }

        return ShapeType.Curve;
    }

    /// <summary>
    /// Làm mịn danh sách điểm theo loại hình dạng đã nhận dạng.
    /// Trả về danh sách điểm mới đã được xử lý.
    /// </summary>
    public static List<Vector2> Smooth(IReadOnlyList<Vector2> rawPoints, ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Horizontal: return SnapToAxis(rawPoints, isHorizontal: true);
            case ShapeType.Vertical:   return SnapToAxis(rawPoints, isHorizontal: false);
            case ShapeType.Diagonal:   return SnapToStraightLine(rawPoints);
            case ShapeType.Curve:      return FitCatmullRomSpline(rawPoints, CurveResolution);
            default:                   return new List<Vector2>(rawPoints);
        }
    }

    /// <summary>
    /// Tính tổng độ dài của một danh sách điểm.
    /// </summary>
    public static float ComputeLength(IReadOnlyList<Vector2> points)
    {
        if (points == null || points.Count < 2) return 0f;
        float total = 0f;
        for (int i = 0; i < points.Count - 1; i++)
            total += Vector2.Distance(points[i], points[i + 1]);
        return total;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NHẬN DẠNG — Tính sai số tuyến tính
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tính sai số tuyến tính trung bình: khoảng cách vuông góc từ mỗi điểm đến
    /// đường thẳng nối điểm đầu và điểm cuối, chuẩn hóa theo tổng chiều dài.
    /// Kết quả ∈ [0, ∞), càng gần 0 = càng thẳng.
    /// </summary>
    private static float ComputeLinearityError(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 2) return 0f;

        Vector2 start = points[0];
        Vector2 end   = points[points.Count - 1];
        Vector2 dir   = end - start;
        float   totalLength = dir.magnitude;

        if (totalLength < 0.0001f)
        {
            // Điểm đầu ≈ điểm cuối — không đủ độ dài để đánh giá
            // Tính bán kính phân tán thay thế
            Vector2 center = start;
            float spread = 0f;
            foreach (var p in points)
                spread += Vector2.Distance(p, center);
            return spread / points.Count;
        }

        Vector2 dirNorm = dir / totalLength;

        float totalError = 0f;
        foreach (var p in points)
        {
            Vector2 toPoint = p - start;
            // Khoảng cách vuông góc (cross product 2D)
            float perpDist = Mathf.Abs(toPoint.x * dirNorm.y - toPoint.y * dirNorm.x);
            totalError += perpDist;
        }

        float avgError = totalError / points.Count;
        return avgError / totalLength; // chuẩn hóa theo chiều dài
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SMOOTH — Đường thẳng
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Snap về đường ngang hoặc dọc hoàn hảo dựa vào trung bình Y hoặc X.</summary>
    private static List<Vector2> SnapToAxis(IReadOnlyList<Vector2> points, bool isHorizontal)
    {
        Vector2 start = points[0];
        Vector2 end   = points[points.Count - 1];

        if (isHorizontal)
        {
            // Lấy Y trung bình để đường nằm giữa quỹ đạo người chơi vẽ
            float avgY = 0f;
            foreach (var p in points) avgY += p.y;
            avgY /= points.Count;
            return new List<Vector2> { new Vector2(start.x, avgY), new Vector2(end.x, avgY) };
        }
        else
        {
            float avgX = 0f;
            foreach (var p in points) avgX += p.x;
            avgX /= points.Count;
            return new List<Vector2> { new Vector2(avgX, start.y), new Vector2(avgX, end.y) };
        }
    }

    /// <summary>Snap về đường thẳng từ điểm đầu đến điểm cuối.</summary>
    private static List<Vector2> SnapToStraightLine(IReadOnlyList<Vector2> points)
    {
        return new List<Vector2> { points[0], points[points.Count - 1] };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SMOOTH — Đường cong Catmull-Rom Spline
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fit Catmull-Rom Spline qua tập điểm thô:
    /// 1. Downsample → control points (loại bỏ nhiễu)
    /// 2. Tính spline qua các control points
    /// 3. Resample đều theo số điểm output mong muốn
    /// </summary>
    private static List<Vector2> FitCatmullRomSpline(IReadOnlyList<Vector2> rawPoints, int resolution)
    {
        // Bước 1: Downsample để lấy control points sạch
        List<Vector2> controlPoints = DownsampleByArcLength(rawPoints, CurveControlPoints);

        if (controlPoints.Count < 2)
            return new List<Vector2>(rawPoints);

        if (controlPoints.Count == 2)
        {
            // Chỉ 2 điểm → vẫn thẳng, không cần spline
            return controlPoints;
        }

        // Bước 2: Thêm điểm phantom ở đầu và cuối để Catmull-Rom không bị hẫng
        var cp = new List<Vector2>(controlPoints);
        Vector2 phantomStart = cp[0] + (cp[0] - cp[1]);
        Vector2 phantomEnd   = cp[cp.Count - 1] + (cp[cp.Count - 1] - cp[cp.Count - 2]);
        cp.Insert(0, phantomStart);
        cp.Add(phantomEnd);

        // Bước 3: Sinh điểm spline
        var splinePoints = new List<Vector2>();
        int segmentCount = cp.Count - 3;
        int stepsPerSegment = Mathf.Max(2, resolution / segmentCount);

        for (int i = 1; i < cp.Count - 2; i++)
        {
            Vector2 p0 = cp[i - 1];
            Vector2 p1 = cp[i];
            Vector2 p2 = cp[i + 1];
            Vector2 p3 = cp[i + 2];

            for (int j = 0; j < stepsPerSegment; j++)
            {
                float t = j / (float)stepsPerSegment;
                splinePoints.Add(CatmullRomInterpolate(p0, p1, p2, p3, t));
            }
        }

        // Đảm bảo điểm cuối chính xác bằng điểm cuối gốc
        splinePoints.Add(controlPoints[controlPoints.Count - 1]);

        return splinePoints;
    }

    /// <summary>Nội suy Catmull-Rom tại tham số t ∈ [0,1] giữa p1 và p2.</summary>
    private static Vector2 CatmullRomInterpolate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER — Downsample theo độ dài cung
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy đều `targetCount` điểm phân bố theo độ dài cung (arc-length parameterization).
    /// Giúp control points phân bố đều, tránh tập trung vào một vùng.
    /// </summary>
    private static List<Vector2> DownsampleByArcLength(IReadOnlyList<Vector2> points, int targetCount)
    {
        if (points.Count <= targetCount)
            return new List<Vector2>(points);

        float totalLength = ComputeLength(points);
        if (totalLength < 0.0001f)
            return new List<Vector2> { points[0] };

        float step = totalLength / (targetCount - 1);
        var result = new List<Vector2> { points[0] };

        float accumulated = 0f;
        float nextTarget  = step;

        for (int i = 0; i < points.Count - 1; i++)
        {
            float segLen = Vector2.Distance(points[i], points[i + 1]);

            while (accumulated + segLen >= nextTarget && result.Count < targetCount - 1)
            {
                float t = (nextTarget - accumulated) / segLen;
                result.Add(Vector2.Lerp(points[i], points[i + 1], t));
                nextTarget += step;
            }

            accumulated += segLen;
        }

        result.Add(points[points.Count - 1]);
        return result;
    }
}
