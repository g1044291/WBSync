using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedWorkDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "planned_work_days",
                table: "tasks",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "planned_work_days",
                table: "tasks");
        }
    }
}
