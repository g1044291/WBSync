using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    task_id = table.Column<int>(type: "INTEGER", nullable: false),
                    assignee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    minutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_logs_assignees_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "assignees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_logs_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_work_logs_assignee_id",
                table: "work_logs",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "idx_work_logs_task_id",
                table: "work_logs",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_logs");
        }
    }
}
