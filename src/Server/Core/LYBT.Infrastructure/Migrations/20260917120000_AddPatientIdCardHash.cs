using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// R-6: Patient.IdCardHash HMAC 盲索引列。
    /// IdNumber 经 AES-GCM 非确定性加密，SQL 等值无法命中；本列存确定性 HMAC-SHA256 供索引精确匹配。
    /// 存量数据由 DatabaseInitializationService / LocalWebApiSeedData 启动回填。
    /// 注：Model snapshot 已同步更新；如需完整 Designer 可执行
    /// `dotnet ef migrations remove` 后重新 `dotnet ef migrations add AddPatientIdCardHash` 生成。
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260917120000_AddPatientIdCardHash")]
    public partial class AddPatientIdCardHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdCardHash",
                table: "Patients",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IdCardHash",
                table: "Patients",
                column: "IdCardHash",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IdCardHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_IdCardHash",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IdCardHash",
                table: "Patients");
        }
    }
}
