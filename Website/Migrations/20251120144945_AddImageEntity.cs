using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamMALsurium.Migrations
{
    /// <inheritdoc />
    public partial class AddImageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalFilename = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Privacy = table.Column<int>(type: "int", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    OriginalPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HighResPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HighResWebPPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MediumResPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MediumResWebPPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThumbnailWebPPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OriginalWidth = table.Column<int>(type: "int", nullable: true),
                    OriginalHeight = table.Column<int>(type: "int", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_Images_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "f608835f-a626-4a5d-b7a3-e8de607b7245");

            migrationBuilder.CreateIndex(
                name: "IX_Images_UploadedAt",
                table: "Images",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Images_UserId",
                table: "Images",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "a3372ba6-162b-4c54-b948-1ba3d57d2947");
        }
    }
}
