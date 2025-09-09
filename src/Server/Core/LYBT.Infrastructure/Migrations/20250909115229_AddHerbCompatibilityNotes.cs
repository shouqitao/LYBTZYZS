using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHerbCompatibilityNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HerbCompatibilityNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbCombination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompatibilityType = table.Column<int>(type: "int", nullable: false),
                    SeverityLevel = table.Column<int>(type: "int", nullable: false),
                    CompatibilityNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DoctorRecommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HerbCompatibilityNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HerbCompatibilityNotes_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HerbCompatibilityNotes_PrescriptionId_IsDeleted",
                table: "HerbCompatibilityNotes",
                columns: new[] { "PrescriptionId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HerbCompatibilityNotes");
        }
    }
}
