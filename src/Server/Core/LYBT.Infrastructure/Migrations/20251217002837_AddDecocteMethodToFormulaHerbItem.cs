using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// 此迁移的更改已合并到InitialCreateV2中。
    /// FormulaHerbItems表现在在InitialCreateV2中创建时就包含DecocteMethod列。
    /// </remarks>
    public partial class AddDecocteMethodToFormulaHerbItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 更改已合并到InitialCreateV2，此迁移为空
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 更改已合并到InitialCreateV2，此迁移为空
        }
    }
}
