using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseOperatorNameMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrinted",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "LastPrintedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrintVersion",
                table: "Prescriptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrinted",
                table: "MedicalCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintedAt",
                table: "MedicalCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "MedicalCases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrintVersion",
                table: "MedicalCases",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "OperatorName",
                table: "MedicalCaseAuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "MedicalCasePrintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintType = table.Column<int>(type: "int", nullable: false),
                    PrintVersion = table.Column<int>(type: "int", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrintedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrintedByName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrinterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCasePrintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCasePrintLogs_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_UserId",
                table: "MedicalCases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCasePrintLogs_MedicalCaseId",
                table: "MedicalCasePrintLogs",
                column: "MedicalCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalCasePrintLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_UserId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "IsPrinted",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "LastPrintedAt",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "PrintVersion",
                table: "MedicalCases");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrinted",
                table: "Prescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrintedAt",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrintVersion",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "OperatorName",
                table: "MedicalCaseAuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
