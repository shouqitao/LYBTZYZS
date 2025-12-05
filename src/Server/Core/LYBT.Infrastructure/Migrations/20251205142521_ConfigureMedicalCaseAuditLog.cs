using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureMedicalCaseAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCaseAuditLogs_MedicalCases_MedicalCaseId",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt",
                table: "MedicalCaseAuditLogs",
                columns: new[] { "MedicalCaseId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseAuditLogs_OperatorId_CreatedAt",
                table: "MedicalCaseAuditLogs",
                columns: new[] { "OperatorId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCaseAuditLogs_MedicalCases_MedicalCaseId",
                table: "MedicalCaseAuditLogs",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCaseAuditLogs_MedicalCases_MedicalCaseId",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCaseAuditLogs_OperatorId_CreatedAt",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId",
                table: "MedicalCaseAuditLogs",
                column: "MedicalCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCaseAuditLogs_MedicalCases_MedicalCaseId",
                table: "MedicalCaseAuditLogs",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
