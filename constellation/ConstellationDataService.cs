namespace ConstellationSketch;

public class ConstellationDataService
{
    private readonly Dictionary<string, IReadOnlyList<BoundaryPoint>> _boundaries = new();
    private readonly Dictionary<int, IReadOnlyList<Star>> _starsByMagnitude = new();

    public ConstellationDataService(IWebHostEnvironment environment)
    {
        var boundaryDir = Path.Combine(environment.WebRootPath, "boundaries");
        foreach (var file in Directory.GetFiles(boundaryDir, "*.txt"))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            var text = File.ReadAllText(file);
            _boundaries[key] = ConstellationRenderer.ParseBoundaryData(text);
        }

        var starPath = Path.Combine(environment.WebRootPath, "star_catalog.txt");
        var starText = File.ReadAllText(starPath);
        var allStars = ConstellationRenderer.ParseStarData(starText);

        for (int mag = 1; mag <= 6; mag++)
        {
            _starsByMagnitude[mag] = allStars.Where(s => s.Magnitude <= mag).ToList();
        }
    }

    public IReadOnlyList<BoundaryPoint>? GetBoundary(string constellation)
    {
        return _boundaries.TryGetValue(constellation, out var points) ? points : null;
    }

    public IReadOnlyList<Star> GetStars(int maxMagnitude)
    {
        return _starsByMagnitude.TryGetValue(maxMagnitude, out var stars) ? stars : [];
    }
}
