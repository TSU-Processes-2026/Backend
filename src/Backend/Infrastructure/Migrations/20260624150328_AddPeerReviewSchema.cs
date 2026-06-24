using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerReviewSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepresentativeAssignedAt",
                table: "teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepresentativeUserId",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewParticipationPolicy",
                table: "teams",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefenseOrder",
                table: "submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "submissions",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalSource",
                table: "submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAt",
                table: "submissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionType",
                table: "submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultReviewTimeLimitMinutes",
                table: "subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LiveReviewMode",
                table: "subjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PairingStrategy",
                table: "subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeerReviewDeadlinePolicy",
                table: "subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PeerReviewEnabled",
                table: "subjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PeerReviewMode",
                table: "subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeerReviewScope",
                table: "subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCriteriaBeforeDeadline",
                table: "subjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TeacherFinalMode",
                table: "subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CriteriaVisibilityAt",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewDeadlineAt",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewEnabled",
                table: "posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewMode",
                table: "posts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewTimeLimitMinutes",
                table: "posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewType",
                table: "posts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TeacherCanEditPeerScores",
                table: "posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TeamReviewPolicy",
                table: "posts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComputedValue",
                table: "criterion_results",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "criterion_results",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId",
                table: "criterion_results",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "final_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalScore = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    FinalGradeString = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FinalSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TeacherOverrideComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_final_grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_final_grades_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewTargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_assignments_posts_TaskId",
                        column: x => x.TaskId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_review_assignments_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    OverallComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                    ReplacedByTeacher = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviews_review_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "review_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_RepresentativeUserId",
                table: "teams",
                column: "RepresentativeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_criterion_results_ReviewId",
                table: "criterion_results",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_final_grades_SubmissionId",
                table: "final_grades",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_review_assignments_SubmissionId",
                table: "review_assignments",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_review_assignments_TaskId",
                table: "review_assignments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_AssignmentId",
                table: "reviews",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_criterion_results_reviews_ReviewId",
                table: "criterion_results",
                column: "ReviewId",
                principalTable: "reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_AspNetUsers_RepresentativeUserId",
                table: "teams",
                column: "RepresentativeUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_criterion_results_reviews_ReviewId",
                table: "criterion_results");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_AspNetUsers_RepresentativeUserId",
                table: "teams");

            migrationBuilder.DropTable(
                name: "final_grades");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "review_assignments");

            migrationBuilder.DropIndex(
                name: "IX_teams_RepresentativeUserId",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_criterion_results_ReviewId",
                table: "criterion_results");

            migrationBuilder.DropColumn(
                name: "RepresentativeAssignedAt",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "RepresentativeUserId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "ReviewParticipationPolicy",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "DefenseOrder",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "FinalSource",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "SubmissionType",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "DefaultReviewTimeLimitMinutes",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "LiveReviewMode",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "PairingStrategy",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "PeerReviewDeadlinePolicy",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "PeerReviewEnabled",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "PeerReviewMode",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "PeerReviewScope",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "ShowCriteriaBeforeDeadline",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "TeacherFinalMode",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "CriteriaVisibilityAt",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ReviewDeadlineAt",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ReviewEnabled",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ReviewMode",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ReviewTimeLimitMinutes",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ReviewType",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "TeacherCanEditPeerScores",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "TeamReviewPolicy",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "ComputedValue",
                table: "criterion_results");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "criterion_results");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "criterion_results");
        }
    }
}
