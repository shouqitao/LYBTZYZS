using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Issue1962_AddHerbCategoryAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除可能存在的IX_Herbs_Name索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Herbs_Name' AND object_id = OBJECT_ID('Herbs'))
                BEGIN
                    DROP INDEX [IX_Herbs_Name] ON [Herbs]
                END
            ");

            // Issue #1962 Task 1.1: 修改Name列长度（100→50，符合BR-001）
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Herbs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // Issue #1962 Task 1.1: 添加Category分类字段
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Herbs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 清理重复的药材名称（保留最早创建的记录，删除重复项）
            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT Id, Name, ROW_NUMBER() OVER (PARTITION BY Name ORDER BY CreatedAt ASC) AS RowNum
                    FROM Herbs
                )
                UPDATE Herbs
                SET IsDeleted = 1
                WHERE Id IN (SELECT Id FROM CTE WHERE RowNum > 1)
            ");

            // Issue #1962 Task 1.1: 创建唯一索引（药材名称）
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Name",
                table: "Herbs",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");  // 仅对非删除记录建立唯一约束

            // 删除可能存在的IX_Herbs_PinYinCode索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Herbs_PinYinCode' AND object_id = OBJECT_ID('Herbs'))
                BEGIN
                    DROP INDEX [IX_Herbs_PinYinCode] ON [Herbs]
                END
            ");

            // Issue #1962 Task 1.1: 创建普通索引（拼音码）
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_PinYinCode",
                table: "Herbs",
                column: "PinYinCode");

            // 删除可能存在的IX_Herbs_Category_Status_Includes索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Herbs_Category_Status_Includes' AND object_id = OBJECT_ID('Herbs'))
                BEGIN
                    DROP INDEX [IX_Herbs_Category_Status_Includes] ON [Herbs]
                END
            ");

            // Issue #1962 Task 1.1: 创建覆盖索引（分类+状态，包含常用查询字段）
            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Category_Status_Includes",
                table: "Herbs",
                columns: new[] { "Category", "Status" })
                .Annotation("SqlServer:Include", new[] { "Name", "PinYinCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚覆盖索引
            migrationBuilder.DropIndex(
                name: "IX_Herbs_Category_Status_Includes",
                table: "Herbs");

            // 回滚拼音码索引
            migrationBuilder.DropIndex(
                name: "IX_Herbs_PinYinCode",
                table: "Herbs");

            // 回滚唯一索引
            migrationBuilder.DropIndex(
                name: "IX_Herbs_Name",
                table: "Herbs");

            // 删除Category列
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Herbs");

            // 恢复Name列长度（50→100）
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Herbs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
