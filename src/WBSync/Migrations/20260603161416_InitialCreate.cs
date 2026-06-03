using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBSync.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "global_holidays",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_holidays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    start_date = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')"),
                    updated_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "assignees",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    project_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')"),
                    updated_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignees", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignees_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignee_holidays",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    assignee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<string>(type: "TEXT", nullable: false),
                    memo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignee_holidays", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignee_holidays_assignees_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "assignees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    project_id = table.Column<int>(type: "INTEGER", nullable: false),
                    parent_id = table.Column<int>(type: "INTEGER", nullable: true),
                    predecessor_id = table.Column<int>(type: "INTEGER", nullable: true),
                    assignee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    work_days = table.Column<double>(type: "REAL", nullable: true),
                    start_date = table.Column<string>(type: "TEXT", nullable: true),
                    end_date = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "未着手"),
                    progress = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')"),
                    updated_at = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now', 'localtime')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.CheckConstraint("CK_tasks_progress", "progress >= 0 AND progress <= 100");
                    table.CheckConstraint("CK_tasks_status", "status IN ('未着手', '進行中', '完了', '保留')");
                    table.ForeignKey(
                        name: "FK_tasks_assignees_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "assignees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tasks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_tasks_parent_id",
                        column: x => x.parent_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_tasks_predecessor_id",
                        column: x => x.predecessor_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_assignee_holidays_assignee_date",
                table: "assignee_holidays",
                columns: new[] { "assignee_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_assignee_holidays_assignee_id",
                table: "assignee_holidays",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "IX_assignees_project_id",
                table: "assignees",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_global_holidays_date",
                table: "global_holidays",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tasks_parent_id",
                table: "tasks",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_tasks_predecessor_id",
                table: "tasks",
                column: "predecessor_id");

            migrationBuilder.CreateIndex(
                name: "idx_tasks_project_id",
                table: "tasks",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_assignee_id",
                table: "tasks",
                column: "assignee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignee_holidays");

            migrationBuilder.DropTable(
                name: "global_holidays");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "assignees");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
