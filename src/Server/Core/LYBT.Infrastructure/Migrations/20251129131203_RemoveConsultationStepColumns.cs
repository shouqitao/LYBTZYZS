using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// 移除Consultation实体的Step时间戳字段
    /// 简化业务流程，移除Step概念
    /// </summary>
    public partial class RemoveConsultationStepColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除索引（如果存在）
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('Consultations')
                    AND name = 'IX_Consultations_Step1CompletedAt'
                )
                BEGIN
                    DROP INDEX [IX_Consultations_Step1CompletedAt] ON [Consultations];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE object_id = OBJECT_ID('Consultations')
                    AND name = 'IX_Consultations_Step2CompletedAt'
                )
                BEGIN
                    DROP INDEX [IX_Consultations_Step2CompletedAt] ON [Consultations];
                END
            ");

            // 再删除列
            migrationBuilder.DropColumn(
                name: "Step1CompletedAt",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Step2CompletedAt",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Step3CompletedAt",
                table: "Consultations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Step1CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Step2CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Step3CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);
        }
    }
}
