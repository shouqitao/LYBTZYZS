using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OpenSpec_SimplifyMedicalCaseDataflow_AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormulaSource",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ConsultationDate",
                table: "MedicalCases");

            migrationBuilder.RenameColumn(
                name: "Indication",
                table: "Prescriptions",
                newName: "Usage");

            migrationBuilder.AddColumn<string>(
                name: "CaseNumber",
                table: "MedicalCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "MedicalCases",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseNumber",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "MedicalCases");

            migrationBuilder.RenameColumn(
                name: "Usage",
                table: "Prescriptions",
                newName: "Indication");

            migrationBuilder.AddColumn<string>(
                name: "FormulaSource",
                table: "Prescriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultationDate",
                table: "MedicalCases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
