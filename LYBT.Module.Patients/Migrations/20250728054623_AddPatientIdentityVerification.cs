using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "Patients",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "Patients");
        }
    }
}
