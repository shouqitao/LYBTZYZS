using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Prescriptions.Migrations {

    /// <inheritdoc />
    public partial class InitialCreate : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<int>(
                name: "DosageCount",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DrugAvailability",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateHerbWarning",
                table: "Prescriptions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaTemplateNames",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAdvice",
                table: "Prescriptions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingHerbs",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SingleDosePrice",
                table: "Prescriptions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Prescriptions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeight",
                table: "Prescriptions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PrescriptionModificationHistory",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModificationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModificationDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PrescriptionModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_PrescriptionModificationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionModificationHistory_Prescriptions_PrescriptionModelId",
                        column: x => x.PrescriptionModelId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionModificationHistory_PrescriptionModelId",
                table: "PrescriptionModificationHistory",
                column: "PrescriptionModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "PrescriptionModificationHistory");

            migrationBuilder.DropColumn(
                name: "DosageCount",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DrugAvailability",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DuplicateHerbWarning",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "FormulaTemplateNames",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MedicalAdvice",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MissingHerbs",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "SingleDosePrice",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "TotalWeight",
                table: "Prescriptions");
        }
    }
}