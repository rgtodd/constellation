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

        public List<SelectListItem> Constellations { get; set; } = [];

        public void OnGet()
        {
            Constellations = ConstellationNames
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => new SelectListItem(kvp.Value, kvp.Key))
                .ToList();
        }
    }
}
