using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Pharmacy.Migrations
{
    /// <inheritdoc />
    public partial class InitialPharmacyMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pharmacies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NeedDecoction = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispenseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HerbModel",
                columns: table => new
                {
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
                    LastOperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PharmacyModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HerbModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HerbModel_Pharmacies_PharmacyModelId",
                        column: x => x.PharmacyModelId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HerbModel_PharmacyModelId",
                table: "HerbModel",
                column: "PharmacyModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_CreateTime",
                table: "Pharmacies",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_PatientId",
                table: "Pharmacies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_PrescriptionId",
                table: "Pharmacies",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_Status",
                table: "Pharmacies",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HerbModel");

            migrationBuilder.DropTable(
                name: "Pharmacies");
        }
    }
}
