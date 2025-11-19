using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamMALsurium.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminAuditLogDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminAuditLogs_AspNetUsers_TargetUserId",
                table: "AdminAuditLogs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "a3372ba6-162b-4c54-b948-1ba3d57d2947");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminAuditLogs_AspNetUsers_TargetUserId",
                table: "AdminAuditLogs",
                column: "TargetUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminAuditLogs_AspNetUsers_TargetUserId",
                table: "AdminAuditLogs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "a1b03bd3-2e2a-4c8a-91ce-4b82eca867b4");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminAuditLogs_AspNetUsers_TargetUserId",
                table: "AdminAuditLogs",
                column: "TargetUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
