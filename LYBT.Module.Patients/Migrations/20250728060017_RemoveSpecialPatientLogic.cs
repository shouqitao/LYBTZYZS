using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Patients.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSpecialPatientLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialPatientDoctors");

            migrationBuilder.DropColumn(
                name: "IsSpecial",
                table: "Patients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpecial",
                table: "Patients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SpecialPatientDoctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialPatientDoctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialPatientDoctors_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctors_DoctorId",
                table: "SpecialPatientDoctors",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctors_PatientId_DoctorId",
                table: "SpecialPatientDoctors",
                columns: new[] { "PatientId", "DoctorId" },
                unique: true);
        }
    }
}
