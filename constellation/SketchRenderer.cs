using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConstellationSketch;

public static class SketchRenderer
{
    public static byte[] Render(string headerMarkdown, string constellationName, double altDegrees, double azDegrees, byte[] pngImageBytes)
    {
        var headerText = headerMarkdown
            .Replace("```", "")
            .Replace("#CONSTELLATION_NAME#", constellationName)
            .Replace("#ALT#", altDegrees.ToString("F0"))
            .Replace("#AZ#", azDegrees.ToString("F0"))
            .Trim();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(0.75f, Unit.Inch);
                page.MarginVertical(0.5f, Unit.Inch);

                page.Content().Column(column =>
                {
                    column.Item().Text(headerText).FontFamily(Fonts.CourierNew).FontSize(11);

                    column.Item().PaddingTop(20).AlignCenter().Image(pngImageBytes);
                });
            });
        });

        return document.GeneratePdf();
    }
}
