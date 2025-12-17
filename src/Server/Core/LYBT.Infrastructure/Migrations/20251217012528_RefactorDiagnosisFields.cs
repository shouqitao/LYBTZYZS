using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// 诊断字段重构迁移
    /// OpenSpec: refactor-diagnosis-fields
    /// 将四个诊断字段（望诊、闻诊、问诊、切诊）合并为"四诊"，新增"舌诊"和"脉诊"
    /// </summary>
    public partial class RefactorDiagnosisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: 添加新字段
            migrationBuilder.AddColumn<string>(
                name: "FourDiagnosis",
                table: "Consultations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TongueDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PulseDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: 数据迁移 - 将旧四诊字段合并到FourDiagnosis
            // 使用SQL Server 2016兼容语法（不使用CONCAT_WS）
            migrationBuilder.Sql(@"
                UPDATE Consultations
                SET FourDiagnosis = STUFF(
                    COALESCE(CASE WHEN Inspection IS NOT NULL AND Inspection != '' THEN CHAR(10) + N'【望诊】' + Inspection END, '') +
                    COALESCE(CASE WHEN AuscultationOlfaction IS NOT NULL AND AuscultationOlfaction != '' THEN CHAR(10) + N'【闻诊】' + AuscultationOlfaction END, '') +
                    COALESCE(CASE WHEN Inquiry IS NOT NULL AND Inquiry != '' THEN CHAR(10) + N'【问诊】' + Inquiry END, '') +
                    COALESCE(CASE WHEN Palpation IS NOT NULL AND Palpation != '' THEN CHAR(10) + N'【切诊】' + Palpation END, ''),
                    1, 1, '')
                WHERE Inspection IS NOT NULL OR AuscultationOlfaction IS NOT NULL
                   OR Inquiry IS NOT NULL OR Palpation IS NOT NULL;
            ");

            // Step 3: 删除旧字段
            migrationBuilder.DropColumn(
                name: "Inspection",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "AuscultationOlfaction",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Inquiry",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Palpation",
                table: "Consultations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: 恢复旧字段
            migrationBuilder.AddColumn<string>(
                name: "Inspection",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuscultationOlfaction",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Inquiry",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Palpation",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: 删除新字段
            migrationBuilder.DropColumn(
                name: "FourDiagnosis",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "TongueDiagnosis",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PulseDiagnosis",
                table: "Consultations");
        }
    }
}
