using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderTaskWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorAcceptedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Tasks] SET [CompletedAt] = [UpdatedAt] WHERE [Status] = 'Done'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SupervisorAcceptedAt",
                table: "Tasks");
        }
    }
}
