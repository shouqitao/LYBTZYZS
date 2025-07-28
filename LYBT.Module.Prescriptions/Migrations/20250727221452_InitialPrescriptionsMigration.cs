using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Prescriptions.Migrations {

    /// <inheritdoc />
    public partial class InitialPrescriptionsMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Usage = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PrescriptionModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionModelId",
                        column: x => x.PrescriptionModelId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_HerbId",
                table: "PrescriptionItems",
                column: "HerbId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionModelId",
                table: "PrescriptionItems",
                column: "PrescriptionModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_CreateTime",
                table: "Prescriptions",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status",
                table: "Prescriptions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "Prescriptions");
        }
    }
}