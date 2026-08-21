using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportRowLevelIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_DoctorId_CreatedAt",
                table: "Registrations",
                columns: new[] { "DoctorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_UserId_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_DoctorId_CreatedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_UserId_CreatedAt",
                table: "MedicalCases");
        }
    }
}
