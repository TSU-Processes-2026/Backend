using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentCriteriaMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "criteria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AppliesTo",
                table: "criteria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "student");

            migrationBuilder.AddColumn<string>(
                name: "CriterionType",
                table: "criteria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenUntilVisibility",
                table: "criteria",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "criteria",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "criteria",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "criteria",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Criterion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliesTo",
                table: "criteria");

            migrationBuilder.DropColumn(
                name: "CriterionType",
                table: "criteria");

            migrationBuilder.DropColumn(
                name: "IsHiddenUntilVisibility",
                table: "criteria");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "criteria");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "criteria");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "criteria");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "criteria",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
