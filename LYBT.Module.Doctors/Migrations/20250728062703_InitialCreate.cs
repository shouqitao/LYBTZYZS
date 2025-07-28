using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Doctors.Migrations {

    /// <inheritdoc />
    public partial class InitialCreate : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "SpecialPatientDoctor");

            migrationBuilder.DropTable(
                name: "PatientModel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "PatientModel",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    AllergyHistory = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisableReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Ethnicity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IDType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsSpecial = table.Column<bool>(type: "bit", nullable: false),
                    MaritalStatus = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WuBiCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_PatientModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialPatientDoctor",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_SpecialPatientDoctor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialPatientDoctor_Doctors_DoctorModelId",
                        column: x => x.DoctorModelId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecialPatientDoctor_PatientModel_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PatientModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctor_DoctorModelId",
                table: "SpecialPatientDoctor",
                column: "DoctorModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctor_PatientId",
                table: "SpecialPatientDoctor",
                column: "PatientId");
        }
    }
}