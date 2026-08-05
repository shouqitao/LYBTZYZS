using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecreateDroppedAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 只补建 SecurityAuditLogs —— MedicalCaseAuditLogs / MedicalCasePrintLogs
            // 已由 AddMedicalCasePrintLog(20260703002757) 重建，此处重建会重复建表
            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                });

            // 复合查询索引：MedicalCaseAuditLogs 表已存在（AddMedicalCasePrintLog 创建），仅补索引
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

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "EventType", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCaseAuditLogs_OperatorId_CreatedAt",
                table: "MedicalCaseAuditLogs");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");
        }
    }
}
