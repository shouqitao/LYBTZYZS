using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationSingleWaitingUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_PatientId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_FormulaHerbItems_FormulaId",
                table: "FormulaHerbItems");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CreatedAt",
                table: "Registrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "UX_Registrations_PatientId_Pending",
                table: "Registrations",
                column: "PatientId",
                unique: true,
                filter: "[Status] IN (0, 1) AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_CreatedAt",
                table: "MedicalCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaHerbItems_FormulaId_HerbId",
                table: "FormulaHerbItems",
                columns: new[] { "FormulaId", "HerbId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_CreatedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "UX_Registrations_PatientId_Pending",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_CreatedAt",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_FormulaHerbItems_FormulaId_HerbId",
                table: "FormulaHerbItems");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_PatientId",
                table: "Registrations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaHerbItems_FormulaId",
                table: "FormulaHerbItems",
                column: "FormulaId");
        }
    }
}
