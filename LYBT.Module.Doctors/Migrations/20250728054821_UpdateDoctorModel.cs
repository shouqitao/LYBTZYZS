using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Doctors.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDoctorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Roles",
                table: "UserModel");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "UserModel",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "UserModel",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "PatientModel",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "UserModel");

            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "UserModel");

            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "PatientModel");

            migrationBuilder.AddColumn<string>(
                name: "Roles",
                table: "UserModel",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
