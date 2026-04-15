using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add missing BaseEntity columns to SecurityAuditLogs table
    /// Issue: CI tests failing with "Invalid column name 'CreatedBy'" for SecurityAuditLog
    /// Root Cause: SecurityAuditLog entity inherits from BaseEntity but table was missing audit columns
    /// Related to: US_AUTH_010_AutoLoginWithValidToken_ReturnsNewSession failure
    /// </summary>
    public partial class AddBaseEntityColumnsToSecurityAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SecurityAuditLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "SecurityAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "SecurityAuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SecurityAuditLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SecurityAuditLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SecurityAuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SecurityAuditLogs");
        }
    }
}
