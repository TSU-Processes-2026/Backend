using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GradingMode",
                table: "subjects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SelfAssessmentEnabled",
                table: "posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SelfAssessmentVisibilityDate",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "course_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalScore = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FinalGrade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_grades_subjects_CourseId",
                        column: x => x.CourseId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MaxPoints = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Points = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    IsBonus = table.Column<bool>(type: "boolean", nullable: false),
                    IsPenalty = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_criteria_posts_TaskId",
                        column: x => x.TaskId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grade_scales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinScore = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubjectId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grade_scales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_grade_scales_subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_grade_scales_subjects_SubjectId1",
                        column: x => x.SubjectId1,
                        principalTable: "subjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "criterion_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssessmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Submissionid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_criterion_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_criterion_results_criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_criterion_results_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_criterion_results_submissions_Submissionid",
                        column: x => x.Submissionid,
                        principalTable: "submissions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_grades_CourseId",
                table: "course_grades",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_criteria_TaskId",
                table: "criteria",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_criterion_results_CriterionId",
                table: "criterion_results",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_criterion_results_Submissionid",
                table: "criterion_results",
                column: "Submissionid");

            migrationBuilder.CreateIndex(
                name: "IX_criterion_results_SubmissionId",
                table: "criterion_results",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_grade_scales_SubjectId",
                table: "grade_scales",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_grade_scales_SubjectId1",
                table: "grade_scales",
                column: "SubjectId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_grades");

            migrationBuilder.DropTable(
                name: "criterion_results");

            migrationBuilder.DropTable(
                name: "grade_scales");

            migrationBuilder.DropTable(
                name: "criteria");

            migrationBuilder.DropColumn(
                name: "GradingMode",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "SelfAssessmentEnabled",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "SelfAssessmentVisibilityDate",
                table: "posts");
        }
    }
}
