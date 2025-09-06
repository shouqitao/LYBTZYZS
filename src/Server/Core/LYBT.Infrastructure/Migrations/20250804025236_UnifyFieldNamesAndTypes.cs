using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations {

    /// <inheritdoc />
    public partial class UnifyFieldNamesAndTypes : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.RenameColumn(
                name: "PinyinCode",
                table: "Users",
                newName: "PinYinCode");

            migrationBuilder.RenameColumn(
                name: "PinyinCode",
                table: "Patients",
                newName: "PinYinCode");

            migrationBuilder.RenameColumn(
                name: "PinyinCode",
                table: "Herbs",
                newName: "PinYinCode");

            migrationBuilder.RenameIndex(
                name: "IX_Herbs_PinyinCode",
                table: "Herbs",
                newName: "IX_Herbs_PinYinCode");

            migrationBuilder.RenameColumn(
                name: "PinyinCode",
                table: "Doctors",
                newName: "PinYinCode");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AdminSecrets",
                newName: "Username");

            migrationBuilder.RenameIndex(
                name: "IX_AdminSecrets_UserName",
                table: "AdminSecrets",
                newName: "IX_AdminSecrets_Username");

            migrationBuilder.AlterColumn<string>(
                name: "Usage",
                table: "Herbs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.RenameColumn(
                name: "PinYinCode",
                table: "Users",
                newName: "PinyinCode");

            migrationBuilder.RenameColumn(
                name: "PinYinCode",
                table: "Patients",
                newName: "PinyinCode");

            migrationBuilder.RenameColumn(
                name: "PinYinCode",
                table: "Herbs",
                newName: "PinyinCode");

            migrationBuilder.RenameIndex(
                name: "IX_Herbs_PinYinCode",
                table: "Herbs",
                newName: "IX_Herbs_PinyinCode");

            migrationBuilder.RenameColumn(
                name: "PinYinCode",
                table: "Doctors",
                newName: "PinyinCode");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AdminSecrets",
                newName: "UserName");

            migrationBuilder.RenameIndex(
                name: "IX_AdminSecrets_Username",
                table: "AdminSecrets",
                newName: "IX_AdminSecrets_UserName");

            migrationBuilder.AlterColumn<string>(
                name: "Usage",
                table: "Herbs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
