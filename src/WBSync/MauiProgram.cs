using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WBSync.Data;
using WBSync.Repositories;
using WBSync.Repositories.Interfaces;
using WBSync.Services;
using WBSync.Services.Interfaces;

namespace WBSync
{
    /// <summary>MAUI アプリケーションのエントリーポイント。</summary>
    public static class MauiProgram
    {
        /// <summary>
        /// MAUI アプリを構築して返す。
        /// DI 登録・SQLite 接続・DB マイグレーション自動適用を行う。
        /// </summary>
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
                options.UseSqlite($"Data Source={dbPath};Foreign Keys=True"));

            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            builder.Services.AddScoped<IAssigneeRepository, AssigneeRepository>();
            builder.Services.AddScoped<IGlobalAssigneeRepository, GlobalAssigneeRepository>();
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<IGlobalHolidayRepository, GlobalHolidayRepository>();
            builder.Services.AddScoped<IAssigneeHolidayRepository, AssigneeHolidayRepository>();
            builder.Services.AddScoped<IWorkLogRepository, WorkLogRepository>();
            builder.Services.AddScoped<IScheduleService, ScheduleService>();

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
