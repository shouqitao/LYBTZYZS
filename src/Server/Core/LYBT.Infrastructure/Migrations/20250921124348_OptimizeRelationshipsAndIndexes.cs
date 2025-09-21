using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeRelationshipsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhoneNumber",
                table: "Patients",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Status_Name",
                table: "Patients",
                columns: new[] { "Status", "Name" },
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_DoctorId_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "DoctorId", "CreatedAt" },
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId_CreatedAt",
                table: "MedicalCases",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Patients_PatientId",
                table: "MedicalCases",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Users_DoctorId",
                table: "MedicalCases",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Patients_PatientId",
                table: "MedicalCases");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Users_DoctorId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PhoneNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_Status_Name",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_DoctorId_CreatedAt",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PatientId_CreatedAt",
                table: "MedicalCases");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases",
                column: "DoctorId");
        }
    }
}
