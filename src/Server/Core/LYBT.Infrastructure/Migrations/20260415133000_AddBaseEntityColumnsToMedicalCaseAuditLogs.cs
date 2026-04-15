using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add missing BaseEntity columns to MedicalCaseAuditLogs table
    /// Issue: CI tests failing with "Invalid column name 'CreatedBy'"
    /// Root Cause: MedicalCaseAuditLog entity inherits from BaseEntity but table was missing audit columns
    /// </summary>
    public partial class AddBaseEntityColumnsToMedicalCaseAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MedicalCaseAuditLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "MedicalCaseAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "MedicalCaseAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MedicalCaseAuditLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MedicalCaseAuditLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MedicalCaseAuditLogs");
        }
    }
}
