using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConsultationDiagnosisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChiefComplaint",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "FourDiagnosis",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "MedicalAdvice",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "TreatmentPrinciple",
                table: "Consultations");

            migrationBuilder.AlterColumn<string>(
                name: "PresentIllness",
                table: "Consultations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PresentIllness",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FourDiagnosis",
                table: "Consultations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAdvice",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentPrinciple",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
