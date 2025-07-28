using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Billing.Migrations {

    /// <inheritdoc />
    public partial class InitialBillingMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "Billings",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BillingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Billings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingItems",
                columns: table => new {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_BillingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingItems_Billings_BillingId",
                        column: x => x.BillingId,
                        principalTable: "Billings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_BillingId",
                table: "BillingItems",
                column: "BillingId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_BillingTime",
                table: "Billings",
                column: "BillingTime");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_PatientId",
                table: "Billings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_Status",
                table: "Billings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_TotalAmount",
                table: "Billings",
                column: "TotalAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "BillingItems");

            migrationBuilder.DropTable(
                name: "Billings");
        }
    }
}