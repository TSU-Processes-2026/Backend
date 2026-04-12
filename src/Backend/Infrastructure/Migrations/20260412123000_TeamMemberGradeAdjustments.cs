using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class TeamMemberGradeAdjustments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team_member_grade_adjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamGradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_member_grade_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_member_grade_adjustments_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_member_grade_adjustments_team_grades_TeamGradeId",
                        column: x => x.TeamGradeId,
                        principalTable: "team_grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_member_grade_adjustments_StudentId",
                table: "team_member_grade_adjustments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_team_member_grade_adjustments_TeamGradeId_StudentId",
                table: "team_member_grade_adjustments",
                columns: new[] { "TeamGradeId", "StudentId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_member_grade_adjustments");
        }
    }
}