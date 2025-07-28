using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Doctors.Migrations {

    /// <inheritdoc />
    public partial class InitialDoctorsMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "PatientModel",
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
                    table.PrimaryKey("PK_PatientModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserModel",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RealName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Roles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WorkStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_UserModel_UserId",
                        column: x => x.UserId,
                        principalTable: "UserModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialPatientDoctor",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                name: "IX_Doctors_PinyinCode",
                table: "Doctors",
                column: "PinyinCode");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Specialty",
                table: "Doctors",
                column: "Specialty");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Status",
                table: "Doctors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Title",
                table: "Doctors",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctor_DoctorModelId",
                table: "SpecialPatientDoctor",
                column: "DoctorModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPatientDoctor_PatientId",
                table: "SpecialPatientDoctor",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "SpecialPatientDoctor");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "PatientModel");

            migrationBuilder.DropTable(
                name: "UserModel");
        }
    }
}