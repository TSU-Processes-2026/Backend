using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionDecisionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecisionDeadlineDays",
                table: "subject_team_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionMode",
                table: "subject_team_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDecision",
                table: "subject_team_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "submission_decision_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_decision_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_decision_sessions_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submission_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionMakerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submission_decisions_AspNetUsers_DecisionMakerId",
                        column: x => x.DecisionMakerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_submission_decisions_submission_decision_sessions_DecisionS~",
                        column: x => x.DecisionSessionId,
                        principalTable: "submission_decision_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_submission_decision_sessions_IsClosed_DeadlineAt",
                table: "submission_decision_sessions",
                columns: new[] { "IsClosed", "DeadlineAt" });

            migrationBuilder.CreateIndex(
                name: "IX_submission_decision_sessions_SubmissionId",
                table: "submission_decision_sessions",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submission_decisions_DecisionMakerId",
                table: "submission_decisions",
                column: "DecisionMakerId");

            migrationBuilder.CreateIndex(
                name: "IX_submission_decisions_DecisionSessionId_DecisionMakerId",
                table: "submission_decisions",
                columns: new[] { "DecisionSessionId", "DecisionMakerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submission_decisions");

            migrationBuilder.DropTable(
                name: "submission_decision_sessions");

            migrationBuilder.DropColumn(
                name: "DecisionDeadlineDays",
                table: "subject_team_settings");

            migrationBuilder.DropColumn(
                name: "DecisionMode",
                table: "subject_team_settings");

            migrationBuilder.DropColumn(
                name: "RequiresDecision",
                table: "subject_team_settings");
        }
    }
}
