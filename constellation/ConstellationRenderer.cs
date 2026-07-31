using SkiaSharp;

namespace WebApplication8;

public record BoundaryPoint(double RaRadians, double DecRadians);

public static class ConstellationRenderer
{
    private const double Deg = Math.PI / 180.0;

    public static IReadOnlyList<BoundaryPoint> ParseBoundaryData(string text)
    {
        var points = new List<BoundaryPoint>();
        foreach (var line in text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length < 2)
                continue;

            var ra = ParseRa(parts[0]);
            var dec = ParseDec(parts[1]);
            points.Add(new BoundaryPoint(ra, dec));
        }
        return points;
    }

    public static byte[] Render(
        IReadOnlyList<BoundaryPoint> boundaryPoints,
        double latitudeDegrees,
        double longitudeDegrees,
        DateTime dateTimeUtc,
        int canvasSize = 600,
        int boundarySize = 550)
    {
        var jd = JulianDate(dateTimeUtc);
        var gmstAngle = Gmst(jd);
        var lst = gmstAngle + longitudeDegrees * Deg;
        var latRad = latitudeDegrees * Deg;

        var center = ComputeCenter(boundaryPoints, latRad, lst);
        var centerProj = StereographicProject(center.Alt, center.Az);

        var centeredPoints = new (double X, double Y)[boundaryPoints.Count];
        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            var (alt, az) = RaDecToAltAz(boundaryPoints[i].RaRadians, boundaryPoints[i].DecRadians, latRad, lst);
            var proj = StereographicProject(alt, az);
            centeredPoints[i] = (proj.X - centerProj.X, proj.Y - centerProj.Y);
        }

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        foreach (var p in centeredPoints)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        var rangeX = maxX - minX;
        var rangeY = maxY - minY;
        var range = Math.Max(rangeX, rangeY);
        var scale = range > 0 ? boundarySize / range : 1.0;

        var midX = (minX + maxX) / 2.0;
        var midY = (minY + maxY) / 2.0;

        var screenPoints = new SKPoint[centeredPoints.Length];
        for (int i = 0; i < centeredPoints.Length; i++)
        {
            screenPoints[i] = new SKPoint(
                (float)(canvasSize / 2.0 + (centeredPoints[i].X - midX) * scale),
                (float)(canvasSize / 2.0 + (centeredPoints[i].Y - midY) * scale));
        }

        using var bitmap = new SKBitmap(canvasSize, canvasSize);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColor.Parse("#AAAAAA"));

        var pathBuilder = new SKPathBuilder();
        pathBuilder.MoveTo(screenPoints[0]);
        for (int i = 1; i < screenPoints.Length; i++)
        {
            pathBuilder.LineTo(screenPoints[i]);
        }
        pathBuilder.Close();
        using var path = pathBuilder.Snapshot();

        using var fillPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawPath(path, fillPaint);

        using var strokePaint = new SKPaint
        {
            Color = SKColor.Parse("#333333"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        canvas.DrawPath(path, strokePaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static double ParseRa(string text)
    {
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var h = double.Parse(parts[0]);
        var m = double.Parse(parts[1]);
        var s = double.Parse(parts[2]);
        return (h + m / 60.0 + s / 3600.0) * 15.0 * Deg;
    }

    private static double ParseDec(string text)
    {
        return double.Parse(text.Trim()) * Deg;
    }

    private static double JulianDate(DateTime dateTimeUtc)
    {
        var y = dateTimeUtc.Year;
        var mo = dateTimeUtc.Month;
        var d = dateTimeUtc.Day;
        var h = dateTimeUtc.Hour + dateTimeUtc.Minute / 60.0 + dateTimeUtc.Second / 3600.0;

        int jy = y;
        int jm = mo;
        if (mo <= 2)
        {
            jy -= 1;
            jm += 12;
        }

        var a = jy / 100;
        var b = 2 - a + a / 4;
        return Math.Floor(365.25 * (jy + 4716)) + Math.Floor(30.6001 * (jm + 1)) + d + h / 24.0 + b - 1524.5;
    }

    private static double Gmst(double jd)
    {
        var t = (jd - 2451545.0) / 36525.0;
        var gmstDeg = 280.46061837 + 360.98564736629 * (jd - 2451545.0)
                      + 0.000387933 * t * t - t * t * t / 38710000.0;
        gmstDeg = ((gmstDeg % 360.0) + 360.0) % 360.0;
        return gmstDeg * Deg;
    }

    private static (double Alt, double Az) RaDecToAltAz(double ra, double dec, double lat, double lst)
    {
        var ha = lst - ra;
        var sinAlt = Math.Sin(dec) * Math.Sin(lat) + Math.Cos(dec) * Math.Cos(lat) * Math.Cos(ha);
        var alt = Math.Asin(Math.Clamp(sinAlt, -1.0, 1.0));

        var cosA = (Math.Sin(dec) - Math.Sin(alt) * Math.Sin(lat)) / (Math.Cos(alt) * Math.Cos(lat));
        var az = Math.Acos(Math.Clamp(cosA, -1.0, 1.0));
        if (Math.Sin(ha) > 0)
        {
            az = 2.0 * Math.PI - az;
        }
        return (alt, az);
    }

    private static (double X, double Y) StereographicProject(double alt, double az)
    {
        var r = Math.Cos(alt) / (1.0 + Math.Sin(alt));
        var x = r * Math.Sin(az);
        var y = -r * Math.Cos(az);
        return (x, y);
    }

    private static (double Alt, double Az) ComputeCenter(IReadOnlyList<BoundaryPoint> points, double lat, double lst)
    {
        double sumRa = 0, sumDec = 0;
        var refRa = points[0].RaRadians;

        foreach (var p in points)
        {
            var dra = p.RaRadians - refRa;
            if (dra > Math.PI) dra -= 2.0 * Math.PI;
            if (dra < -Math.PI) dra += 2.0 * Math.PI;
            sumRa += refRa + dra;
            sumDec += p.DecRadians;
        }

        var centerRa = sumRa / points.Count;
        var centerDec = sumDec / points.Count;

        return RaDecToAltAz(centerRa, centerDec, lat, lst);
    }
}
