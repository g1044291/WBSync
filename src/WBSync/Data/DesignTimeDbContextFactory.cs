using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WBSync.Data;

// EF Core CLI ツール（dotnet ef）がデザイン時にDbContextを生成するためのファクトリ。
// MAUI のマルチターゲット構成ではツールが MauiProgram を解析できないため必要。
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "db", "wbsync.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new AppDbContext(options);
    }
}
