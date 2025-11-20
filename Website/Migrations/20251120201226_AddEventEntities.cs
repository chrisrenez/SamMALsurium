using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SamMALsurium.Migrations
{
    /// <inheritdoc />
    public partial class AddEventEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ForumThreadId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Events",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Events",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizedBy",
                table: "Events",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RsvpEnabled",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "EventAnnouncements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAnnouncements_AspNetUsers_SentBy",
                        column: x => x.SentBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAnnouncements_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventArtworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ArtworkId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventArtworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventArtworks_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RsvpStatus = table.Column<int>(type: "int", nullable: false),
                    RsvpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttendees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMedia_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "21928cac-f014-48ab-8610-91ce2d63d72c");

            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Hands-on learning sessions focused on specific artistic techniques or skills", "Workshop" },
                    { 2, "Art shows and gallery exhibitions showcasing member artwork", "Exhibition" },
                    { 3, "Informal gatherings for artists to connect and socialize", "Meetup" },
                    { 4, "Virtual learning sessions and webinars", "Online Class" },
                    { 5, "Constructive feedback sessions for artwork and creative projects", "Critique Session" },
                    { 6, "Other types of events and gatherings", "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive",
                table: "Events",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsActive_StartDate",
                table: "Events",
                columns: new[] { "IsActive", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublic",
                table: "Events",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublic_StartDate",
                table: "Events",
                columns: new[] { "IsPublic", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizedBy",
                table: "Events",
                column: "OrganizedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAnnouncements_EventId",
                table: "EventAnnouncements",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAnnouncements_SentAt",
                table: "EventAnnouncements",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventAnnouncements_SentBy",
                table: "EventAnnouncements",
                column: "SentBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventArtworks_ArtworkId",
                table: "EventArtworks",
                column: "ArtworkId");

            migrationBuilder.CreateIndex(
                name: "IX_EventArtworks_EventId",
                table: "EventArtworks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventArtworks_EventId_ArtworkId",
                table: "EventArtworks",
                columns: new[] { "EventId", "ArtworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId_UserId",
                table: "EventAttendees",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_RsvpStatus",
                table: "EventAttendees",
                column: "RsvpStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UserId",
                table: "EventAttendees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMedia_EventId",
                table: "EventMedia",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMedia_EventId_DisplayOrder",
                table: "EventMedia",
                columns: new[] { "EventId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_OrganizedBy",
                table: "Events",
                column: "OrganizedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventTypes_EventTypeId",
                table: "Events",
                column: "EventTypeId",
                principalTable: "EventTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_OrganizedBy",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventTypes_EventTypeId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "EventAnnouncements");

            migrationBuilder.DropTable(
                name: "EventArtworks");

            migrationBuilder.DropTable(
                name: "EventAttendees");

            migrationBuilder.DropTable(
                name: "EventMedia");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventTypeId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsActive_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublic",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublic_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_OrganizedBy",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ForumThreadId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OrganizedBy",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RsvpEnabled",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Events",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "92b501c7-16b3-4f2e-98ac-5ca7c02e9650");
        }
    }
}
