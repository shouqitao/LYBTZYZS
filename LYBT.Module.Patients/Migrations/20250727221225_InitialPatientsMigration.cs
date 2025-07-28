using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Patients.Migrations {

    /// <inheritdoc />
    public partial class InitialPatientsMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllergyHistory = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Ethnicity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IDType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MaritalStatus = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisableReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsSpecial = table.Column<bool>(type: "bit", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialPatientDoctors",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_SpecialPatientDoctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialPatientDoctors_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CreatedAt",
                table: "Patients",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IDNumber",
                table: "Patients",
                column: "IDNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Name_Status",
                table: "Patients",
                columns: new[] { "Name", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhoneNumber",
                table: "Patients",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PinyinCode",
                table: "Patients",
                column: "PinyinCode");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Status",
                table: "Patients",
                column: "Status");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "SpecialPatientDoctors");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}