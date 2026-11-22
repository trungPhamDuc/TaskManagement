using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManagement.Data;

#nullable disable

namespace TaskManagement.Migrations;

[DbContext(typeof(TpmDbContext))]
[Migration("20261118090000_AddNotifices")]
public partial class AddNotifices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notifices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                WorkTaskId = table.Column<int>(type: "int", nullable: false),
                IsRead = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifices", x => x.Id);
                table.ForeignKey("FK_Notifices_Tasks_WorkTaskId", x => x.WorkTaskId, "Tasks", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Notifices_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Notifices_UserId_IsRead", "Notifices", new[] { "UserId", "IsRead" });
        migrationBuilder.CreateIndex("IX_Notifices_WorkTaskId", "Notifices", "WorkTaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Notifices");
}
