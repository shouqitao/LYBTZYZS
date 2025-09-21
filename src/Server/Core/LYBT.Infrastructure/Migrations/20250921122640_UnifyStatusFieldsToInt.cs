using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyStatusFieldsToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "MedicalCases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[Status] = 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MedicalCases",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[Status] = 'Active' OR [Status] = 'Draft'");
        }
    }
}
