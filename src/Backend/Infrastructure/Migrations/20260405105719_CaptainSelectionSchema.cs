using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CaptainSelectionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CaptainSelectedAt",
                table: "teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CaptainUserId",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectionMethod",
                table: "teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptainSelectionMode",
                table: "subject_team_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaptainVotingDeadlineDays",
                table: "subject_team_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresCaptain",
                table: "subject_team_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "captain_voting_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_captain_voting_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_captain_voting_sessions_AspNetUsers_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_captain_voting_sessions_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "captain_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VotingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedForUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_captain_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_captain_votes_AspNetUsers_VotedForUserId",
                        column: x => x.VotedForUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_captain_votes_AspNetUsers_VoterId",
                        column: x => x.VoterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_captain_votes_captain_voting_sessions_VotingSessionId",
                        column: x => x.VotingSessionId,
                        principalTable: "captain_voting_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_CaptainUserId",
                table: "teams",
                column: "CaptainUserId");

            migrationBuilder.CreateIndex(
                name: "IX_captain_votes_VotedForUserId",
                table: "captain_votes",
                column: "VotedForUserId");

            migrationBuilder.CreateIndex(
                name: "IX_captain_votes_VoterId",
                table: "captain_votes",
                column: "VoterId");

            migrationBuilder.CreateIndex(
                name: "IX_captain_votes_VotingSessionId_VoterId",
                table: "captain_votes",
                columns: new[] { "VotingSessionId", "VoterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_captain_voting_sessions_TeamId_IsClosed",
                table: "captain_voting_sessions",
                columns: new[] { "TeamId", "IsClosed" });

            migrationBuilder.CreateIndex(
                name: "IX_captain_voting_sessions_WinnerId",
                table: "captain_voting_sessions",
                column: "WinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_AspNetUsers_CaptainUserId",
                table: "teams",
                column: "CaptainUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_AspNetUsers_CaptainUserId",
                table: "teams");

            migrationBuilder.DropTable(
                name: "captain_votes");

            migrationBuilder.DropTable(
                name: "captain_voting_sessions");

            migrationBuilder.DropIndex(
                name: "IX_teams_CaptainUserId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "CaptainSelectedAt",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "CaptainUserId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "SelectionMethod",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "CaptainSelectionMode",
                table: "subject_team_settings");

            migrationBuilder.DropColumn(
                name: "CaptainVotingDeadlineDays",
                table: "subject_team_settings");

            migrationBuilder.DropColumn(
                name: "RequiresCaptain",
                table: "subject_team_settings");
        }
    }
}
