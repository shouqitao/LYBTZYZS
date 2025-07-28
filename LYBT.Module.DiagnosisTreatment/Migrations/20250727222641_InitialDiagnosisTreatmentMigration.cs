using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.DiagnosisTreatment.Migrations {

    /// <inheritdoc />
    public partial class InitialDiagnosisTreatmentMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "DiagnosisTreatments",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosisCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Formula_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DiagnosisTreatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisTreatmentFormulaHerbs",
                columns: table => new {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiagnosisTreatmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DiagnosisTreatmentFormulaHerbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisTreatmentFormulaHerbs_DiagnosisTreatments_DiagnosisTreatmentId",
                        column: x => x.DiagnosisTreatmentId,
                        principalTable: "DiagnosisTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisTreatmentItems",
                columns: table => new {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiagnosisTreatmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DiagnosisTreatmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisTreatmentItems_DiagnosisTreatments_DiagnosisTreatmentId",
                        column: x => x.DiagnosisTreatmentId,
                        principalTable: "DiagnosisTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisTreatmentFormulaHerbs_DiagnosisTreatmentId",
                table: "DiagnosisTreatmentFormulaHerbs",
                column: "DiagnosisTreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisTreatmentItems_DiagnosisTreatmentId",
                table: "DiagnosisTreatmentItems",
                column: "DiagnosisTreatmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "DiagnosisTreatmentFormulaHerbs");

            migrationBuilder.DropTable(
                name: "DiagnosisTreatmentItems");

            migrationBuilder.DropTable(
                name: "DiagnosisTreatments");
        }
    }
}