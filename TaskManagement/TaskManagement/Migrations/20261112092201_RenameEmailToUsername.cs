using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations;

public partial class RenameEmailToUsername : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");
        migrationBuilder.RenameColumn(name: "Email", table: "Users", newName: "Username");
        migrationBuilder.AlterColumn<string>(name: "Username", table: "Users", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(256)", oldMaxLength: 256);
        migrationBuilder.CreateIndex(name: "IX_Users_Username", table: "Users", column: "Username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Users_Username", table: "Users");
        migrationBuilder.AlterColumn<string>(name: "Username", table: "Users", type: "nvarchar(256)", maxLength: 256, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100);
        migrationBuilder.RenameColumn(name: "Username", table: "Users", newName: "Email");
        migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true);
    }
}
