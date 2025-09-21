using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMedicalCasePerDocumentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Prescriptions_PrescriptionId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PrescriptionId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_AdminSecrets_Username",
                table: "AdminSecrets");

            migrationBuilder.DropColumn(
                name: "PrescriptionId",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AdminSecrets");

            migrationBuilder.RenameIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations",
                newName: "UX_Consultations_MedicalCaseId");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Prescriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "PrescriptionItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Patients",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MedicalCases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "MedicalCases",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MedicalCases",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Consultations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Consultations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoggerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Prescriptions_MedicalCaseId",
                table: "Prescriptions",
                column: "MedicalCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_CreatedAt",
                table: "MedicalCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[Status] = 'Active' OR [Status] = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Level",
                table: "SystemLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_LoggerName",
                table: "SystemLogs",
                column: "LoggerName");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Timestamp",
                table: "SystemLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Timestamp_Level",
                table: "SystemLogs",
                columns: new[] { "Timestamp", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_UserId",
                table: "SystemLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_MedicalCases_MedicalCaseId",
                table: "Prescriptions",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_MedicalCases_MedicalCaseId",
                table: "Prescriptions");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropIndex(
                name: "UX_Prescriptions_MedicalCaseId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_CreatedAt",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Consultations");

            migrationBuilder.RenameIndex(
                name: "UX_Consultations_MedicalCaseId",
                table: "Consultations",
                newName: "IX_Consultations_MedicalCaseId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PrescriptionItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionId",
                table: "MedicalCases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AdminSecrets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AdminSecrets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "Username",
                value: "sysadmin");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PrescriptionId",
                table: "MedicalCases",
                column: "PrescriptionId",
                unique: true,
                filter: "[PrescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdminSecrets_Username",
                table: "AdminSecrets",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Prescriptions_PrescriptionId",
                table: "MedicalCases",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id");
        }
    }
}
