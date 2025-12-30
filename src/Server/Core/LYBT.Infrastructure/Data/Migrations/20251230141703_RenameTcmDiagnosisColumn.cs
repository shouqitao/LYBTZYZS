using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTcmDiagnosisColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 重命名 TCMDiagnosis 列为 TcmDiagnosis（遵循C#命名规范）
            migrationBuilder.RenameColumn(
                name: "TCMDiagnosis",
                table: "Consultations",
                newName: "TcmDiagnosis");

            // 注意: AutoLoginTokens表已在之前的迁移中创建，此处跳过
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TcmDiagnosis",
                table: "Consultations",
                newName: "TCMDiagnosis");
        }
    }
}
