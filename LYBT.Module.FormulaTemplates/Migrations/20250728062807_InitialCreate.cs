using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.FormulaTemplates.Migrations {

    /// <inheritdoc />
    public partial class InitialCreate : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "FormulaTemplates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FormulaTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FormulaTemplates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "FormulaTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "FormulaTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FormulaTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "FormulaTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SharedAt",
                table: "FormulaTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SharedById",
                table: "FormulaTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FormulaTemplates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "SharedAt",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "SharedById",
                table: "FormulaTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FormulaTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "FormulaTemplates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FormulaTemplates",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}