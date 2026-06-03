using Microsoft.EntityFrameworkCore;
using WBSync.Models;

namespace WBSync.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Assignee> Assignees => Set<Assignee>();
    public DbSet<WbsTask> Tasks => Set<WbsTask>();
    public DbSet<GlobalHoliday> GlobalHolidays => Set<GlobalHoliday>();
    public DbSet<AssigneeHoliday> AssigneeHolidays => Set<AssigneeHoliday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");
            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");
        });

        modelBuilder.Entity<Assignee>(entity =>
        {
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Assignees)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");
            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");
        });

        modelBuilder.Entity<WbsTask>(entity =>
        {
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Tasks)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Parent)
                  .WithMany(t => t.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // predecessor は自己参照（Cascade 不可のため Restrict → SET NULL はマイグレーションで設定）
            entity.HasOne(e => e.Predecessor)
                  .WithMany()
                  .HasForeignKey(e => e.PredecessorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Assignee)
                  .WithMany(a => a.Tasks)
                  .HasForeignKey(e => e.AssigneeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Status)
                  .HasDefaultValue("未着手");
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_tasks_status",
                "status IN ('未着手', '進行中', '完了', '保留')"));

            entity.Property(e => e.Progress)
                  .HasDefaultValue(0);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_tasks_progress",
                "progress >= 0 AND progress <= 100"));

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");
            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("DATETIME('now', 'localtime')");

            entity.HasIndex(e => e.ProjectId).HasDatabaseName("idx_tasks_project_id");
            entity.HasIndex(e => e.ParentId).HasDatabaseName("idx_tasks_parent_id");
            entity.HasIndex(e => e.PredecessorId).HasDatabaseName("idx_tasks_predecessor_id");
        });

        modelBuilder.Entity<GlobalHoliday>(entity =>
        {
            entity.HasIndex(e => e.Date)
                  .IsUnique()
                  .HasDatabaseName("idx_global_holidays_date");
        });

        modelBuilder.Entity<AssigneeHoliday>(entity =>
        {
            entity.HasOne(e => e.Assignee)
                  .WithMany(a => a.Holidays)
                  .HasForeignKey(e => e.AssigneeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.AssigneeId, e.Date })
                  .IsUnique()
                  .HasDatabaseName("idx_assignee_holidays_assignee_date");

            entity.HasIndex(e => e.AssigneeId)
                  .HasDatabaseName("idx_assignee_holidays_assignee_id");
        });
    }
}
