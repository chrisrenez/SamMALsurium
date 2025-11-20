using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamMALsurium.Migrations
{
    /// <inheritdoc />
    public partial class AddPollIndexesOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "92b501c7-16b3-4f2e-98ac-5ca7c02e9650");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_EndDate",
                table: "Polls",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_EventId_Status",
                table: "Polls",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Polls_Status_EndDate",
                table: "Polls",
                columns: new[] { "Status", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Polls_EndDate",
                table: "Polls");

            migrationBuilder.DropIndex(
                name: "IX_Polls_EventId_Status",
                table: "Polls");

            migrationBuilder.DropIndex(
                name: "IX_Polls_Status_EndDate",
                table: "Polls");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "28fcf42f-4270-4634-a674-b01fe03b7ed3");
        }
    }
}
