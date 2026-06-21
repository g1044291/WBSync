using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class AddProductivityCoefficient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "productivity_coefficient",
                table: "global_assignees",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "productivity_coefficient",
                table: "assignees",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "productivity_coefficient",
                table: "global_assignees");

            migrationBuilder.DropColumn(
                name: "productivity_coefficient",
                table: "assignees");
        }
    }
}
