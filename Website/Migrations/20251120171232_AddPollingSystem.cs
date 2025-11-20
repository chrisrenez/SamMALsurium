using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SamMALsurium.Migrations
{
    /// <inheritdoc />
    public partial class AddPollingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: true),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsMultiSelect = table.Column<bool>(type: "bit", nullable: false),
                    ScoreMin = table.Column<int>(type: "int", nullable: false),
                    ScoreMax = table.Column<int>(type: "int", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    ResultsVisibility = table.Column<int>(type: "int", nullable: false),
                    AllowVoteChange = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Polls_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Polls_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVoteHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreviousVoteJson = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVoteHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVoteHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVoteHistories_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes_AvailabilityGrid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Availability = table.Column<int>(type: "int", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes_AvailabilityGrid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVotes_AvailabilityGrid_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_AvailabilityGrid_PollOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "PollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_AvailabilityGrid_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes_MultipleChoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes_MultipleChoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVotes_MultipleChoice_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_MultipleChoice_PollOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "PollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_MultipleChoice_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes_RankedChoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes_RankedChoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVotes_RankedChoice_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_RankedChoice_PollOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "PollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_RankedChoice_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes_ScoreVoting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes_ScoreVoting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollVotes_ScoreVoting_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_ScoreVoting_PollOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "PollOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PollVotes_ScoreVoting_Polls_PollId",
                        column: x => x.PollId,
                        principalTable: "Polls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "d0ef58dd-5df3-4226-a836-a122291feb08");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedById",
                table: "Events",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDate",
                table: "Events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_PollOptions_PollId",
                table: "PollOptions",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_CreatedAt",
                table: "Polls",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_CreatedById",
                table: "Polls",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_EventId",
                table: "Polls",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_Status",
                table: "Polls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PollVoteHistories_PollId_UserId_ChangedAt",
                table: "PollVoteHistories",
                columns: new[] { "PollId", "UserId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PollVoteHistories_UserId",
                table: "PollVoteHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_AvailabilityGrid_OptionId",
                table: "PollVotes_AvailabilityGrid",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_AvailabilityGrid_PollId_UserId_OptionId",
                table: "PollVotes_AvailabilityGrid",
                columns: new[] { "PollId", "UserId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_AvailabilityGrid_UserId",
                table: "PollVotes_AvailabilityGrid",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_MultipleChoice_OptionId",
                table: "PollVotes_MultipleChoice",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_MultipleChoice_PollId_UserId_OptionId",
                table: "PollVotes_MultipleChoice",
                columns: new[] { "PollId", "UserId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_MultipleChoice_UserId",
                table: "PollVotes_MultipleChoice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_RankedChoice_OptionId",
                table: "PollVotes_RankedChoice",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_RankedChoice_PollId_UserId_OptionId",
                table: "PollVotes_RankedChoice",
                columns: new[] { "PollId", "UserId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_RankedChoice_PollId_UserId_Rank",
                table: "PollVotes_RankedChoice",
                columns: new[] { "PollId", "UserId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_RankedChoice_UserId",
                table: "PollVotes_RankedChoice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_ScoreVoting_OptionId",
                table: "PollVotes_ScoreVoting",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_ScoreVoting_PollId_UserId_OptionId",
                table: "PollVotes_ScoreVoting",
                columns: new[] { "PollId", "UserId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_ScoreVoting_UserId",
                table: "PollVotes_ScoreVoting",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PollVoteHistories");

            migrationBuilder.DropTable(
                name: "PollVotes_AvailabilityGrid");

            migrationBuilder.DropTable(
                name: "PollVotes_MultipleChoice");

            migrationBuilder.DropTable(
                name: "PollVotes_RankedChoice");

            migrationBuilder.DropTable(
                name: "PollVotes_ScoreVoting");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "f608835f-a626-4a5d-b7a3-e8de607b7245");
        }
    }
}
