using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConstellationSketch;

public static class SketchRenderer
{
    private const string DEFAULT_FONT = Fonts.CourierNew;
    private const int DEFAULT_FONT_SIZE = 11;

    public static byte[] Render(string headerMarkdown, string constellationName, double altDegrees, double azDegrees, DateTimeOffset observationTime, byte[] pngImageBytes)
    {
        var headerText = headerMarkdown
            .Replace("```", "")
            .Replace("#CONSTELLATION_NAME#", constellationName)
            .Replace("#ALT#", altDegrees.ToString("F0"))
            .Replace("#AZ#", azDegrees.ToString("F0"))
            .Replace("#DATE#", observationTime.ToString("yyyy-MM-dd").PadRight(10))
            .Replace("#TIME#", observationTime.ToString("h:mm tt").PadRight(10))
            .Trim();

        var footerText = "© 2026 Richard Todd - Constellation Sketch";

        var footerUrl = "https://constellation-sketch.azurewebsites.net";

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
                    column.Item().Text(headerText).FontFamily(DEFAULT_FONT).FontSize(DEFAULT_FONT_SIZE);

                    column.Item().PaddingTop(20).AlignCenter().Image(pngImageBytes);

                    column.Item().PaddingTop(20).Text(text => text.Span(footerText).FontFamily(DEFAULT_FONT).FontSize(DEFAULT_FONT_SIZE));

                    column.Item().Text(text => text.Hyperlink(footerUrl, footerUrl).FontFamily(DEFAULT_FONT).FontSize(DEFAULT_FONT_SIZE));
                });
            });
        });

        return document.GeneratePdf();
    }
}
