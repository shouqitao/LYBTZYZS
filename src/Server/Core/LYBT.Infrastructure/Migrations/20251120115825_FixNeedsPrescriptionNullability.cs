using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// Epic #2175 BF-002 Phase 1: 修复NeedsPrescription字段为nullable
    /// 
    /// 问题：旧Migration错误地将NeedsPrescription定义为NOT NULL DEFAULT 0
    /// 修复：改为NULL，支持三态语义：
    ///   - null: 未标记（用户还未做Step 2决策）
    ///   - true: 需要开处方
    ///   - false: 不需要开处方（明确决策）
    /// 
    /// 数据迁移：将现有的false(0)值转为null（未标记状态）
    /// </summary>
    public partial class FixNeedsPrescriptionNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Epic #2175 BF-002: 数据迁移 - 将现有的false(0)值转为null（未标记状态）
            // null表示"未标记"，false表示"明确不需要开处方"，这是两个不同的语义
            migrationBuilder.Sql(@"
                UPDATE [MedicalCases]
                SET [NeedsPrescription] = NULL
                WHERE [NeedsPrescription] = 0;
            ");

            // 修改字段类型为nullable
            migrationBuilder.AlterColumn<bool>(
                name: "NeedsPrescription",
                table: "MedicalCases",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "NeedsPrescription",
                table: "MedicalCases",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }
    }
}
