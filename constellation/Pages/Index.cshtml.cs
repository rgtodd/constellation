using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication8.Pages
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
            ["sercp"] = "Serpens (Caput)",
            ["sercd"] = "Serpens (Cauda)",
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
        [DataType(DataType.DateTime)]
        public DateTime DateTime { get; set; } = new DateTime(DateTime.Now.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);

        [BindProperty]
        public string Constellation { get; set; } = "and";

        public string? RenderedImageDataUrl { get; set; }

        public void OnGet()
        {
            PopulateConstellations();
        }

        public IActionResult OnPost()
        {
            PopulateConstellations();

            var pngBytes = RenderConstellationImage();
            if (pngBytes is null)
                return Page();

            RenderedImageDataUrl = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
            return Page();
        }

        public IActionResult OnPostDownloadImage()
        {
            PopulateConstellations();

            var pngBytes = RenderConstellationImage();
            if (pngBytes is null)
                return Page();

            var fileName = $"{Constellation.ToUpper()}_SKETCH.png";

            return File(pngBytes, "image/png", fileName);
        }

        public IActionResult OnPostDownloadSketch()
        {
            PopulateConstellations();

            var pngBytes = RenderConstellationImage();
            if (pngBytes is null)
                return Page();

            var headerPath = Path.Combine(_environment.ContentRootPath, "sketch_header.md");
            var headerMarkdown = System.IO.File.ReadAllText(headerPath);
            var constellationName = ConstellationNames[Constellation];

            var pdfBytes = SketchRenderer.Render(headerMarkdown, constellationName, pngBytes);
            var fileName = $"{Constellation.ToUpper()}_SKETCH.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        private byte[]? RenderConstellationImage()
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

            var dateTimeUtc = DateTime.ToUniversalTime();
            return ConstellationRenderer.Render(points, Latitude, Longitude, dateTimeUtc);
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
