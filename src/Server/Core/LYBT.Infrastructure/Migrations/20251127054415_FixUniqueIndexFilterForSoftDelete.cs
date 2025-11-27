using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueIndexFilterForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            // Bug Fix: 添加IsDeleted=0条件，避免软删除的Active医案阻止新建
            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[CaseStatus] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases");

            // 回滚：恢复原索引（不包含IsDeleted条件）
            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[CaseStatus] = 1");
        }
    }
}
