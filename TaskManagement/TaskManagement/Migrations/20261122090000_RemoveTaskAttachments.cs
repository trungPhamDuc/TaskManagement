using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManagement.Data;

#nullable disable

namespace TaskManagement.Migrations;

[DbContext(typeof(TpmDbContext))]
[Migration("20261122090000_RemoveTaskAttachments")]
public partial class RemoveTaskAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TaskAttachments");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TaskAttachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WorkTaskId = table.Column<int>(type: "int", nullable: false),
                StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Kind = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskAttachments", x => x.Id);
                table.ForeignKey("FK_TaskAttachments_Tasks_WorkTaskId", x => x.WorkTaskId, "Tasks", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_TaskAttachments_WorkTaskId", "TaskAttachments", "WorkTaskId");
    }
}
