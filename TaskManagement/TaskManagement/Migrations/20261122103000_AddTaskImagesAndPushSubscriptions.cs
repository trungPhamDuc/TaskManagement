using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManagement.Data;

#nullable disable

namespace TaskManagement.Migrations;

[DbContext(typeof(TpmDbContext))]
[Migration("20261122103000_AddTaskImagesAndPushSubscriptions")]
public partial class AddTaskImagesAndPushSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PushSubscriptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                Endpoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                EndpointHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                P256dh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Auth = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                table.ForeignKey("FK_PushSubscriptions_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TaskImages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WorkTaskId = table.Column<int>(type: "int", nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                Kind = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskImages", x => x.Id);
                table.ForeignKey("FK_TaskImages_Tasks_WorkTaskId", x => x.WorkTaskId, "Tasks", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_PushSubscriptions_EndpointHash", "PushSubscriptions", "EndpointHash", unique: true);
        migrationBuilder.CreateIndex("IX_PushSubscriptions_UserId", "PushSubscriptions", "UserId");
        migrationBuilder.CreateIndex("IX_TaskImages_WorkTaskId", "TaskImages", "WorkTaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PushSubscriptions");
        migrationBuilder.DropTable(name: "TaskImages");
    }
}
