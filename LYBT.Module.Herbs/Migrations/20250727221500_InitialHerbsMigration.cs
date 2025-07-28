using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Herbs.Migrations {

    /// <inheritdoc />
    public partial class InitialHerbsMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "Herbs",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Spec = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Effect = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastOperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Herbs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_CreatedAt",
                table: "Herbs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Name",
                table: "Herbs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_PinyinCode",
                table: "Herbs",
                column: "PinyinCode");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Status",
                table: "Herbs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "Herbs");
        }
    }
}