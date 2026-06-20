using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "global_assignee_id",
                table: "assignees",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "global_assignees",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_assignees", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignees_global_assignee_id",
                table: "assignees",
                column: "global_assignee_id");

            migrationBuilder.CreateIndex(
                name: "idx_global_assignees_name",
                table: "global_assignees",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_assignees_global_assignees_global_assignee_id",
                table: "assignees",
                column: "global_assignee_id",
                principalTable: "global_assignees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assignees_global_assignees_global_assignee_id",
                table: "assignees");

            migrationBuilder.DropTable(
                name: "global_assignees");

            migrationBuilder.DropIndex(
                name: "IX_assignees_global_assignee_id",
                table: "assignees");

            migrationBuilder.DropColumn(
                name: "global_assignee_id",
                table: "assignees");
        }
    }
}
