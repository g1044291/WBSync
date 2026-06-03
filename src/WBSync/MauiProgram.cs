using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WBSync.Data;

namespace WBSync
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            var dbDir = Path.Combine(AppContext.BaseDirectory, "db");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "wbsync.db");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.MigrateAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}
