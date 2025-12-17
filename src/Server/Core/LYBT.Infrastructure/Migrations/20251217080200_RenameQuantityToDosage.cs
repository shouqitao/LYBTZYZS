using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameQuantityToDosage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 重命名 FormulaHerbItems 表的 Quantity 列为 Dosage
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "FormulaHerbItems",
                newName: "Dosage");

            // 重命名 PrescriptionItems 表的 Quantity 列为 Dosage
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "PrescriptionItems",
                newName: "Dosage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚: 将 Dosage 列改回 Quantity
            migrationBuilder.RenameColumn(
                name: "Dosage",
                table: "FormulaHerbItems",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Dosage",
                table: "PrescriptionItems",
                newName: "Quantity");
        }
    }
}
