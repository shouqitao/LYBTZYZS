using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations {

    /// <inheritdoc />
    public partial class RemoveDepartmentFields : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "Age",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "WorkStatus",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "WuBiCode",
                table: "Doctors",
                newName: "Introduction");

            migrationBuilder.RenameColumn(
                name: "Birthday",
                table: "Doctors",
                newName: "UpdateTime");

            migrationBuilder.AlterColumn<string>(
                name: "Specialty",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "Doctors",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                table: "Doctors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "Doctors",
                newName: "Birthday");

            migrationBuilder.RenameColumn(
                name: "Introduction",
                table: "Doctors",
                newName: "WuBiCode");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "MedicalCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Specialty",
                table: "Doctors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNumber",
                table: "Doctors",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Title",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkStatus",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
