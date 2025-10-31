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
            // 1. 添加NeedsPrescription字段（幂等性：仅在列不存在时添加）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('MedicalCases')
                    AND name = 'NeedsPrescription'
                )
                BEGIN
                    ALTER TABLE [MedicalCases] ADD [NeedsPrescription] bit NOT NULL DEFAULT 0;
                END
            ");

            // 2. 添加Status索引（幂等性：仅在索引不存在时创建）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('MedicalCases')
                    AND name = 'IX_MedicalCases_Status'
                )
                BEGIN
                    CREATE INDEX [IX_MedicalCases_Status] ON [MedicalCases] ([Status]);
                END
            ");

            // 3. 添加PatientId+Status复合索引（幂等性：仅在索引不存在时创建）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('MedicalCases')
                    AND name = 'IX_MedicalCases_PatientId_Status'
                )
                BEGIN
                    CREATE INDEX [IX_MedicalCases_PatientId_Status] ON [MedicalCases] ([PatientId], [Status]);
                END
            ");
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
