using SkiaSharp;

namespace WebApplication8;

public record BoundaryPoint(double RaRadians, double DecRadians);

public static class ConstellationRenderer
{
    private const double DEGREES_TO_RADIANS = Math.PI / 180.0;

    /// <summary>
    /// Parses constellation boundary data from a pipe-delimited text format.
    /// </summary>
    /// <param name="text">
    /// Multi-line string where each line contains a boundary vertex in the format "HH MM SS.ss|DD.ddddd".
    /// The right ascension (RA) field contains hours, minutes, and seconds separated by spaces.
    /// The declination (Dec) field is a decimal degree value. Fields are separated by a pipe character.
    /// </param>
    /// <returns>
    /// A list of <see cref="BoundaryPoint"/> records, each containing RA and Dec converted to radians.
    /// RA is converted from hours/minutes/seconds to radians via (h + m/60 + s/3600) * 15 * pi/180.
    /// Dec is converted from decimal degrees to radians via deg * pi/180.
    /// </returns>
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

    /// <summary>
    /// Renders a constellation boundary polygon onto a square PNG image using stereographic projection.
    /// </summary>
    /// <param name="boundaryPoints">
    /// Constellation boundary vertices as (RA, Dec) pairs in radians, as returned by <see cref="ParseBoundaryData"/>.
    /// </param>
    /// <param name="latitudeDegrees">Observer's geographic latitude in degrees (positive = north, negative = south).</param>
    /// <param name="longitudeDegrees">Observer's geographic longitude in degrees (positive = east, negative = west).</param>
    /// <param name="dateTimeUtc">Observation time in UTC, used to compute the Julian Date and sidereal time.</param>
    /// <param name="canvasSize">Width and height of the output square image in pixels (default 600).</param>
    /// <param name="boundarySize">
    /// Maximum span in pixels that the projected boundary polygon should occupy within the canvas (default 550).
    /// The polygon is uniformly scaled so its largest dimension (X or Y) fits within this size.
    /// </param>
    /// <returns>A byte array containing the PNG-encoded image.</returns>
    /// <remarks>
    /// The rendering pipeline is:
    /// 1. Compute the Julian Date from <paramref name="dateTimeUtc"/>, then derive Greenwich Mean Sidereal Time (GMST).
    /// 2. Compute Local Sidereal Time (LST) = GMST + longitude (in radians).
    /// 3. Find the centroid of the boundary in (RA, Dec) space (wrapping RA around 2*pi), convert it to (Alt, Az),
    ///    and stereographically project it to obtain a center offset.
    /// 4. Convert each boundary point from (RA, Dec) to horizontal coordinates (Alt, Az) using the observer's
    ///    latitude and LST, then apply a zenithal stereographic projection: r = cos(alt)/(1+sin(alt)),
    ///    x = r*sin(az), y = -r*cos(az). Subtract the center projection to keep the polygon centered.
    /// 5. Compute the bounding box of the centered projected points, determine a uniform scale factor
    ///    so the largest axis fits within <paramref name="boundarySize"/> pixels, then map to screen coordinates
    ///    centered on the canvas.
    /// 6. Draw the polygon as a filled white region with a dark stroke onto a gray background, encode as PNG.
    /// </remarks>
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
        var lst = gmstAngle + longitudeDegrees * DEGREES_TO_RADIANS;
        var latRad = latitudeDegrees * DEGREES_TO_RADIANS;

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

    /// <summary>
    /// Parses a right ascension string in sexagesimal format and converts it to radians.
    /// </summary>
    /// <param name="text">
    /// A string in the format "HH MM SS.ss" where HH is hours (0-23), MM is minutes (0-59),
    /// and SS.ss is seconds (0-59.99).
    /// </param>
    /// <returns>
    /// Right ascension in radians (0 to 2*pi). The conversion is:
    /// decimal_hours = h + m/60 + s/3600, then radians = decimal_hours * 15 * (pi/180).
    /// The factor of 15 converts hours to degrees (360 degrees / 24 hours = 15 degrees/hour).
    /// </returns>
    private static double ParseRa(string text)
    {
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var h = double.Parse(parts[0]);
        var m = double.Parse(parts[1]);
        var s = double.Parse(parts[2]);
        return (h + m / 60.0 + s / 3600.0) * 15.0 * DEGREES_TO_RADIANS;
    }

    /// <summary>
    /// Parses a declination value from a decimal degree string and converts it to radians.
    /// </summary>
    /// <param name="text">Declination in decimal degrees (e.g., "+45.5" or "-30.25"). Range: -90 to +90.</param>
    /// <returns>
    /// Declination in radians (-pi/2 to +pi/2). The conversion is: radians = degrees * (pi/180).
    /// </returns>
    private static double ParseDec(string text)
    {
        return double.Parse(text.Trim()) * DEGREES_TO_RADIANS;
    }

    /// <summary>
    /// Computes the Julian Date (JD) from a UTC DateTime using the algorithm from Meeus, "Astronomical Algorithms."
    /// </summary>
    /// <param name="dateTimeUtc">The date and time in UTC.</param>
    /// <returns>
    /// The Julian Date as a continuous day count from the epoch January 1, 4713 BC (Julian calendar).
    /// For example, J2000.0 (2000-01-01 12:00 UTC) = JD 2451545.0.
    /// </returns>
    /// <remarks>
    /// Algorithm:
    /// 1. If month &lt;= 2, treat as month 13 or 14 of the previous year (shift year-1, month+12).
    /// 2. Compute Gregorian calendar correction: A = floor(year/100), B = 2 - A + floor(A/4).
    /// 3. JD = floor(365.25 * (year + 4716)) + floor(30.6001 * (month + 1)) + day + hour/24 + B - 1524.5.
    /// The constant 4716 and 1524.5 offset to the Julian epoch. The 365.25 factor accounts for leap years;
    /// 30.6001 approximates month lengths (using month+1 to index correctly).
    /// </remarks>
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

    /// <summary>
    /// Computes Greenwich Mean Sidereal Time (GMST) from a Julian Date.
    /// </summary>
    /// <param name="jd">Julian Date (e.g., 2451545.0 for J2000.0).</param>
    /// <returns>
    /// GMST in radians (0 to 2*pi), representing the angle of the vernal equinox
    /// relative to the Greenwich meridian.
    /// </returns>
    /// <remarks>
    /// Uses the IAU formula referenced to the J2000.0 epoch (JD 2451545.0):
    /// 1. Compute T = (JD - 2451545.0) / 36525.0, the number of Julian centuries since J2000.0.
    /// 2. GMST (degrees) = 280.46061837 + 360.98564736629 * (JD - 2451545.0)
    ///    + 0.000387933 * T^2 - T^3 / 38710000.0.
    ///    The dominant term (360.985...) reflects Earth's sidereal rotation rate (~366.25 rotations/year).
    ///    The T^2 and T^3 terms correct for precession and higher-order effects.
    /// 3. Normalize to [0, 360) degrees via modulo, then convert to radians.
    /// </remarks>
    private static double Gmst(double jd)
    {
        var t = (jd - 2451545.0) / 36525.0;
        var gmstDeg = 280.46061837 + 360.98564736629 * (jd - 2451545.0)
                      + 0.000387933 * t * t - t * t * t / 38710000.0;
        gmstDeg = ((gmstDeg % 360.0) + 360.0) % 360.0;
        return gmstDeg * DEGREES_TO_RADIANS;
    }

    /// <summary>
    /// Converts equatorial coordinates (RA, Dec) to horizontal coordinates (Altitude, Azimuth)
    /// for a given observer latitude and local sidereal time.
    /// </summary>
    /// <param name="ra">Right ascension in radians (0 to 2*pi).</param>
    /// <param name="dec">Declination in radians (-pi/2 to +pi/2).</param>
    /// <param name="lat">Observer's geographic latitude in radians (-pi/2 to +pi/2, positive = north).</param>
    /// <param name="lst">Local Sidereal Time in radians (0 to 2*pi).</param>
    /// <returns>
    /// A tuple (Alt, Az) where:
    /// - Alt = altitude in radians (-pi/2 to +pi/2), the angle above (+) or below (-) the horizon.
    /// - Az = azimuth in radians (0 to 2*pi), measured from north (0) through east (pi/2), south (pi), west (3*pi/2).
    /// </returns>
    /// <remarks>
    /// Algorithm:
    /// 1. Hour Angle: HA = LST - RA. This is the angular distance of the object west of the local meridian.
    /// 2. Altitude: sin(alt) = sin(dec)*sin(lat) + cos(dec)*cos(lat)*cos(HA).
    ///    This is the standard spherical trigonometry formula for the angular height above the horizon.
    /// 3. Azimuth: cos(A) = (sin(dec) - sin(alt)*sin(lat)) / (cos(alt)*cos(lat)).
    ///    A = acos(cos(A)), then if sin(HA) > 0, flip to Az = 2*pi - A to place the azimuth in the correct
    ///    hemisphere (objects with positive hour angle are west of the meridian).
    /// Values are clamped to [-1, 1] before asin/acos to guard against floating-point rounding errors.
    /// </remarks>
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

    /// <summary>
    /// Projects horizontal coordinates (altitude, azimuth) onto a 2D plane using a zenithal (polar)
    /// stereographic projection centered on the zenith.
    /// </summary>
    /// <param name="alt">Altitude in radians (-pi/2 to +pi/2). The zenith (alt = pi/2) projects to the origin.</param>
    /// <param name="az">Azimuth in radians (0 to 2*pi), measured from north through east.</param>
    /// <returns>
    /// A tuple (X, Y) in projection-plane coordinates (dimensionless, range approximately -1 to 1 for
    /// objects above the horizon). The zenith maps to (0, 0); the horizon maps to a unit circle.
    /// +X points east, +Y points south (north is -Y).
    /// </returns>
    /// <remarks>
    /// The stereographic projection from the nadir (opposite the zenith) maps the celestial hemisphere onto
    /// a plane tangent to the zenith:
    /// 1. Radial distance: r = cos(alt) / (1 + sin(alt)).
    ///    At the zenith (alt = pi/2), r = 0. At the horizon (alt = 0), r = 1.
    ///    This is derived from the standard stereographic formula r = tan((pi/2 - alt)/2),
    ///    rewritten using the half-angle identity.
    /// 2. Cartesian coordinates: x = r * sin(az), y = -r * cos(az).
    ///    The negative sign on y orients the projection so that north is upward (negative Y direction
    ///    in screen-like coordinates where Y increases downward).
    /// This projection preserves angles (conformal) and maps circles on the sphere to circles on the plane.
    /// </remarks>
    private static (double X, double Y) StereographicProject(double alt, double az)
    {
        var r = Math.Cos(alt) / (1.0 + Math.Sin(alt));
        var x = r * Math.Sin(az);
        var y = -r * Math.Cos(az);
        return (-x, y);
    }

    /// <summary>
    /// Computes the centroid of a set of boundary points in equatorial coordinates,
    /// then converts it to horizontal (Alt, Az) coordinates for the observer.
    /// </summary>
    /// <param name="points">Constellation boundary vertices with RA and Dec in radians.</param>
    /// <param name="lat">Observer's geographic latitude in radians.</param>
    /// <param name="lst">Local Sidereal Time in radians.</param>
    /// <returns>
    /// The (Altitude, Azimuth) in radians of the centroid of the boundary polygon, representing
    /// the center of the constellation as seen from the observer's location and time.
    /// </returns>
    /// <remarks>
    /// RA values wrap around at 2*pi (0h = 24h), so a simple arithmetic mean would produce incorrect
    /// results for constellations that straddle the 0h/24h boundary. To handle this:
    /// 1. Use the first point's RA as a reference.
    /// 2. For each subsequent point, compute delta_RA = RA - reference_RA, then wrap delta_RA into
    ///    the range (-pi, +pi] by adding or subtracting 2*pi. This ensures nearby points contribute
    ///    nearby values regardless of the wrap boundary.
    /// 3. Accumulate (reference_RA + wrapped_delta) for each point, then divide by the count.
    /// Declination does not wrap, so it is averaged directly.
    /// The resulting mean (RA, Dec) is then converted to (Alt, Az) via <see cref="RaDecToAltAz"/>.
    /// </remarks>
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
