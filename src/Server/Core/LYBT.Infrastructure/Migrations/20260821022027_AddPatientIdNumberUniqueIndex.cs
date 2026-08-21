using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientIdNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Patients_IdNumber",
                table: "Patients",
                column: "IdNumber",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IdNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Formulas_Name",
                table: "Formulas",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_IdNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Formulas_Name",
                table: "Formulas");
        }
    }
}
