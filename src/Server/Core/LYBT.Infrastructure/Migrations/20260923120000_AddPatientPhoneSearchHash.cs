using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// R-6（2026-09-23）：Patient.PhoneSearchHash HMAC 盲索引列。
    /// <para>背景缺陷：关键词检索对 AES-GCM 加密的 PhoneNumber 做 <c>Contains</c>，EF 会把参数一并加密后生成
    /// <c>LIKE @p ESCAPE N'&lt;密文&gt;'</c>；密文含非法转义字符时 SQL Server 报
    /// 「invalid escape character … LIKE predicate」→ 患者搜索 500（US-PAT-001 状态 ⚠️ 取证）。
    /// 同时 <c>ExistsByPhoneAsync</c> 的加密列等值比较因随机 nonce 永不相等 → 电话查重恒 false。</para>
    /// <para>本列存确定性 HMAC-SHA256（与 <c>IdCardHash</c> 同模式），供索引精确匹配；存量数据由
    /// <c>DatabaseInitializationService</c> / <c>LocalWebApiSeedData</c> 启动回填（密文无法在 SQL 内计算 HMAC）。</para>
    /// <para>非唯一索引：手机号唯一性由服务层 <c>ExistsByPhoneAsync</c> 保障；DB 唯一约束会因存量重复数据
    /// 导致迁移失败，属独立的数据治理项。</para>
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260923120000_AddPatientPhoneSearchHash")]
    public partial class AddPatientPhoneSearchHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneSearchHash",
                table: "Patients",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhoneSearchHash",
                table: "Patients",
                column: "PhoneSearchHash",
                filter: "[IsDeleted] = 0 AND [PhoneSearchHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_PhoneSearchHash",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PhoneSearchHash",
                table: "Patients");
        }
    }
}
