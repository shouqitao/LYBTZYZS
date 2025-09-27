using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// 添加处方打印管理功能 - 简化版
    /// 仅包含处方打印版本控制和打印日志记录
    /// </summary>
    public partial class SimplifiedPrescriptionPrintManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 向Prescriptions表添加打印管理字段
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
                defaultValue: 1);

            // 2. 创建处方打印日志表
            migrationBuilder.CreateTable(
                name: "PrescriptionPrintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PrescriptionPrintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionPrintLogs_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. 创建索引以优化查询
            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionPrintLogs_PrescriptionId",
                table: "PrescriptionPrintLogs",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionPrintLogs_PrintedAt",
                table: "PrescriptionPrintLogs",
                column: "PrintedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_PrescriptionPrintLogs_PrescriptionId",
                table: "PrescriptionPrintLogs");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionPrintLogs_PrintedAt",
                table: "PrescriptionPrintLogs");

            // 删除打印日志表
            migrationBuilder.DropTable(
                name: "PrescriptionPrintLogs");

            // 删除Prescriptions表的打印管理字段
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
        }
    }
}