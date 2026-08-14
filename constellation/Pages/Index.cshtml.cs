using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConstellationSketch.Pages
{
    public class IndexModel : PageModel
    {
        private static readonly Dictionary<string, string> ConstellationNames = new()
        {
            ["and"] = "Andromeda",
            ["aql"] = "Aquila",
            ["aqr"] = "Aquarius",
            ["ari"] = "Aries",
            ["aur"] = "Auriga",
            ["boo"] = "Boötes",
            ["cam"] = "Camelopardalis",
            ["cas"] = "Cassiopeia",
            ["cep"] = "Cepheus",
            ["cmi"] = "Canis Minor",
            ["cnc"] = "Cancer",
            ["com"] = "Coma Berenices",
            ["crb"] = "Corona Borealis",
            ["cvn"] = "Canes Venatici",
            ["cyg"] = "Cygnus",
            ["del"] = "Delphinus",
            ["dra"] = "Draco",
            ["equ"] = "Equuleus",
            ["gem"] = "Gemini",
            ["her"] = "Hercules",
            ["lac"] = "Lacerta",
            ["leo"] = "Leo",
            ["lmi"] = "Leo Minor",
            ["lyn"] = "Lynx",
            ["lyr"] = "Lyra",
            ["mon"] = "Monoceros",
            ["ori"] = "Orion",
            ["peg"] = "Pegasus",
            ["per"] = "Perseus",
            ["psc"] = "Pisces",
            ["ser1"] = "Serpens (Caput)",
            ["ser2"] = "Serpens (Cauda)",
            ["sex"] = "Sextans",
            ["sge"] = "Sagitta",
            ["tau"] = "Taurus",
            ["tri"] = "Triangulum",
            ["uma"] = "Ursa Major",
            ["umi"] = "Ursa Minor",
            ["vul"] = "Vulpecula"
        };

        private readonly IWebHostEnvironment _environment;

        public IndexModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public List<SelectListItem> Constellations { get; set; } = [];

        [BindProperty]
        public double Latitude { get; set; } = 40.7128;

        [BindProperty]
        public double Longitude { get; set; } = -74.0060;

        [BindProperty]
        public double Elevation { get; set; } = 0;

        [BindProperty]
        [DataType(DataType.DateTime)]
        public DateTime DateTime { get; set; } = new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);

        [BindProperty]
        [Display(Name = "UTC Offset")]
        public string UtcOffset { get; set; } = "+00:00";

        public List<SelectListItem> UtcOffsets { get; set; } = [];

        [BindProperty]
        public string Constellation { get; set; } = "and";

        public string? RenderedImageDataUrl { get; set; }

        public void OnGet()
        {
            PopulateConstellations();
            PopulateUtcOffsets();
        }

        public IActionResult OnPost()
        {
            PopulateConstellations();
            PopulateUtcOffsets();

            var result = RenderConstellationImage();
            if (result is null)
                return Page();

            RenderedImageDataUrl = $"data:image/png;base64,{Convert.ToBase64String(result.PngBytes)}";
            return Page();
        }

        public IActionResult OnPostDownloadImage()
        {
            PopulateConstellations();
            PopulateUtcOffsets();

            var result = RenderConstellationImage();
            if (result is null)
                return Page();

            var fileName = $"{Constellation.ToUpper()}_SKETCH.png";

            return File(result.PngBytes, "image/png", fileName);
        }

        public IActionResult OnPostDownloadSketch()
        {
            PopulateConstellations();
            PopulateUtcOffsets();

            var result = RenderConstellationImage();
            if (result is null)
                return Page();

            var headerPath = Path.Combine(_environment.ContentRootPath, "sketch_header.md");
            var headerMarkdown = System.IO.File.ReadAllText(headerPath);
            var constellationName = ConstellationNames[Constellation];

            var offset = TimeSpan.Parse(UtcOffset.Replace("+", ""));
            var observationTime = new DateTimeOffset(DateTime, offset);

            var pdfBytes = SketchRenderer.Render(headerMarkdown, constellationName, result.CenterAltDegrees, result.CenterAzDegrees, observationTime, result.PngBytes);
            var fileName = $"{Constellation.ToUpper()}_SKETCH.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        private RenderResult? RenderConstellationImage()
        {
            if (!ConstellationNames.ContainsKey(Constellation))
            {
                ModelState.AddModelError(nameof(Constellation), "Invalid constellation.");
                return null;
            }

            var boundaryPath = Path.Combine(_environment.WebRootPath, "boundaries", $"{Constellation}.txt");
            if (!System.IO.File.Exists(boundaryPath))
            {
                ModelState.AddModelError(nameof(Constellation), "Boundary data not found.");
                return null;
            }

            var text = System.IO.File.ReadAllText(boundaryPath);
            var points = ConstellationRenderer.ParseBoundaryData(text);

            var offset = TimeSpan.Parse(UtcOffset.Replace("+", ""));
            var dateTimeUtc = new DateTimeOffset(DateTime, offset).UtcDateTime;
            return ConstellationRenderer.Render(points, Latitude, Longitude, dateTimeUtc, Elevation);
        }

        private void PopulateUtcOffsets()
        {
            var offsets = new[]
            {
                "-12:00", "-11:00", "-10:00", "-09:30", "-09:00",
                "-08:00", "-07:00", "-06:00", "-05:00", "-04:00",
                "-03:30", "-03:00", "-02:00", "-01:00", "+00:00",
                "+01:00", "+02:00", "+03:00", "+03:30", "+04:00",
                "+04:30", "+05:00", "+05:30", "+05:45", "+06:00",
                "+06:30", "+07:00", "+08:00", "+08:45", "+09:00",
                "+09:30", "+10:00", "+10:30", "+11:00", "+12:00",
                "+12:45", "+13:00", "+14:00"
            };

            UtcOffsets = offsets
                .Select(o => new SelectListItem($"UTC{o}", o))
                .ToList();
        }

        private void PopulateConstellations()
        {
            Constellations = ConstellationNames
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => new SelectListItem(kvp.Value, kvp.Key))
                .ToList();
        }
    }
}
