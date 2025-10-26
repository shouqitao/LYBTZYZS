using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// Epic #1612 Task 1.1: 添加NeedsPrescription标志字段
    /// 支持动态流程控制,用户可选择是否开处方
    /// 同时添加Status和PatientId+Status索引优化查询性能
    /// </summary>
    public partial class AddNeedsPrescriptionFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 添加NeedsPrescription字段
            migrationBuilder.AddColumn<bool>(
                name: "NeedsPrescription",
                table: "MedicalCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2. 添加Status索引（优化查询性能）
            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_Status",
                table: "MedicalCases",
                column: "Status");

            // 3. 添加PatientId+Status复合索引（优化按患者查询病案）
            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId_Status",
                table: "MedicalCases",
                columns: new[] { "PatientId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_Status",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PatientId_Status",
                table: "MedicalCases");

            // 删除字段
            migrationBuilder.DropColumn(
                name: "NeedsPrescription",
                table: "MedicalCases");
        }
    }
}
