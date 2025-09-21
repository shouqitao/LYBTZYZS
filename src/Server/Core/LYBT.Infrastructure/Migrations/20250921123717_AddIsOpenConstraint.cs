using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOpenConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenComputed",
                table: "MedicalCases",
                type: "bit",
                nullable: true,
                computedColumnSql: "CASE WHEN [Status] = 'Active' THEN CAST(1 AS BIT) ELSE NULL END");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_OneActive",
                table: "MedicalCases",
                columns: new[] { "PatientId", "IsOpenComputed" },
                unique: true,
                filter: "[IsOpenComputed] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_OneActive",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "IsOpenComputed",
                table: "MedicalCases");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[Status] = 'Active' OR [Status] = 'Draft'");
        }
    }
}
