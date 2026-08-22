using Microsoft.AspNetCore.ResponseCompression;
using QuestPDF.Infrastructure;

namespace ConstellationSketch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddSingleton<ConstellationDataService>();
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/png", "application/pdf"]);
            });

            var app = builder.Build();

            // Force eager initialization of the data service so the first request doesn't pay the cost.
            app.Services.GetRequiredService<ConstellationDataService>();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseResponseCompression();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
