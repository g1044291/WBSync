using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLogComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "work_logs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment",
                table: "work_logs");
        }
    }
}
