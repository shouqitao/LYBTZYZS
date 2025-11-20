using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// Epic #2175 Task 1.1: 添加BF-002相关索引
    /// 为NeedsPrescription和Step时间戳字段创建索引，优化查询性能
    /// </summary>
    public partial class AddBF002Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 添加MedicalCases.NeedsPrescription索引（带过滤条件，仅索引非null值）
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('MedicalCases')
                    AND name = 'IX_MedicalCases_NeedsPrescription_Filtered'
                )
                BEGIN
                    CREATE INDEX [IX_MedicalCases_NeedsPrescription_Filtered]
                    ON [MedicalCases] ([NeedsPrescription])
                    WHERE [NeedsPrescription] = 1;
                END
            ");

            // 2. 添加Consultations.Step1CompletedAt索引
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('Consultations')
                    AND name = 'IX_Consultations_Step1CompletedAt'
                )
                BEGIN
                    CREATE INDEX [IX_Consultations_Step1CompletedAt]
                    ON [Consultations] ([Step1CompletedAt]);
                END
            ");

            // 3. 添加Consultations.Step2CompletedAt索引
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('Consultations')
                    AND name = 'IX_Consultations_Step2CompletedAt'
                )
                BEGIN
                    CREATE INDEX [IX_Consultations_Step2CompletedAt]
                    ON [Consultations] ([Step2CompletedAt]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除索引
            migrationBuilder.DropIndex(
                name: "IX_Consultations_Step2CompletedAt",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_Step1CompletedAt",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_NeedsPrescription_Filtered",
                table: "MedicalCases");
        }
    }
}
