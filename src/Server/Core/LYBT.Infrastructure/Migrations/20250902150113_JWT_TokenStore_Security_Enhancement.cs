using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{

    /// <inheritdoc />
    public partial class JWT_TokenStore_Security_Enhancement : Migration
    {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Consultations_ConsultationId",
                table: "MedicalCases");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionModelId",
                table: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DiagnosisCatalogs");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "InfrastructureLogs");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "PerformanceLogs");

            migrationBuilder.DropTable(
                name: "SecurityLogs");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "UserActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionModelId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_ConsultationId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_CreateTime",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_ConsultationTime",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_AuthSessions_Username",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DuplicateWarning",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MissingDrugWarning",
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

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrescriptionModelId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CompleteTime",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "LastOperatorId",
                table: "Herbs");

            migrationBuilder.DropColumn(
                name: "LastOperatorName",
                table: "Herbs");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "AllergyHistory",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "ConsultationTime",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "DiagnosisCatalogId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "DiastolicPressure",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "HeartRate",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PastHistory",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PhysicalExamination",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PulseCondition",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "RespiratoryRate",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "SystolicPressure",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "TongueInspection",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "WesternDiagnosis",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "AnomaliesDescription",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "ClientIp",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "ExtendedData",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "GeoLocation",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "HasAnomalies",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "IsAutoLogout",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "IsTokenRevoked",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "JwtTokenHash",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityTime",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "LastRefreshTime",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "LoginType",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RefreshCount",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RevokeReason",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RevokeTime",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "ServerInfo",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "TokenExpiryTime",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AuthSessions");

            migrationBuilder.RenameColumn(
                name: "Age",
                table: "Patients",
                newName: "MaritalStatus");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MedicalCases",
                newName: "DoctorId");

            migrationBuilder.RenameColumn(
                name: "CreateTime",
                table: "MedicalCases",
                newName: "ConsultationDate");

            migrationBuilder.RenameColumn(
                name: "ConsultationId",
                table: "MedicalCases",
                newName: "PrescriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalCases_UserId",
                table: "MedicalCases",
                newName: "IX_MedicalCases_DoctorId");

            migrationBuilder.RenameColumn(
                name: "RememberMe",
                table: "AuthSessions",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "CreateTime",
                table: "AuthSessions",
                newName: "ExpiryTime");

            migrationBuilder.AlterColumn<string>(
                name: "PinYinCode",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FormulaSource",
                table: "Prescriptions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Advice",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Prescriptions",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Indication",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalCaseId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "IdType",
                table: "Patients",
                type: "int",
                maxLength: 20,
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BloodType",
                table: "Patients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelation",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorName",
                table: "MedicalCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "MedicalCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Herbs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "Herbs",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentPrinciple",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TCMDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuthSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuthSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AuthSessions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuthSessions",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "AuthSessions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "RefreshTokenStore",
                columns: table => new
                {
                    RefreshToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccessTokenId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsLongTerm = table.Column<bool>(type: "bit", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokenStore", x => x.RefreshToken);
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousTokenActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "Medium"),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    IsHandled = table.Column<bool>(type: "bit", nullable: false),
                    HandledNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HandledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousTokenActivity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenStore",
                columns: table => new
                {
                    TokenId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "access_token"),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenStore", x => x.TokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PrescriptionId",
                table: "MedicalCases",
                column: "PrescriptionId",
                unique: true,
                filter: "[PrescriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenStore_CreatedAt",
                table: "RefreshTokenStore",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenStore_ExpiresAt",
                table: "RefreshTokenStore",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenStore_IsUsed_IsRevoked_ExpiresAt",
                table: "RefreshTokenStore",
                columns: new[] { "IsUsed", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenStore_UserId",
                table: "RefreshTokenStore",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_ActivityType",
                table: "SuspiciousTokenActivity",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_ClientIP_CreatedAt",
                table: "SuspiciousTokenActivity",
                columns: new[] { "ClientIP", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_CreatedAt",
                table: "SuspiciousTokenActivity",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_IsHandled",
                table: "SuspiciousTokenActivity",
                column: "IsHandled");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_RiskScore",
                table: "SuspiciousTokenActivity",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_Severity",
                table: "SuspiciousTokenActivity",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousTokenActivity_UserId_CreatedAt",
                table: "SuspiciousTokenActivity",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenStore_CreatedAt",
                table: "TokenStore",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TokenStore_ExpiresAt",
                table: "TokenStore",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TokenStore_IsRevoked",
                table: "TokenStore",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_TokenStore_UserId",
                table: "TokenStore",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenStore_UserId_IsRevoked_ExpiresAt",
                table: "TokenStore",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Prescriptions_PrescriptionId",
                table: "MedicalCases",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCases_Prescriptions_PrescriptionId",
                table: "MedicalCases");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "RefreshTokenStore");

            migrationBuilder.DropTable(
                name: "SuspiciousTokenActivity");

            migrationBuilder.DropTable(
                name: "TokenStore");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCases_PrescriptionId",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Indication",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MedicalCaseId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelation",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "DoctorName",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "AuthSessions");

            migrationBuilder.RenameColumn(
                name: "MaritalStatus",
                table: "Patients",
                newName: "Age");

            migrationBuilder.RenameColumn(
                name: "PrescriptionId",
                table: "MedicalCases",
                newName: "ConsultationId");

            migrationBuilder.RenameColumn(
                name: "DoctorId",
                table: "MedicalCases",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ConsultationDate",
                table: "MedicalCases",
                newName: "CreateTime");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases",
                newName: "IX_MedicalCases_UserId");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                table: "AuthSessions",
                newName: "RememberMe");

            migrationBuilder.RenameColumn(
                name: "ExpiryTime",
                table: "AuthSessions",
                newName: "CreateTime");

            migrationBuilder.AlterColumn<string>(
                name: "PinYinCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FormulaSource",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Advice",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Prescriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateWarning",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingDrugWarning",
                table: "Prescriptions",
                type: "nvarchar(max)",
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

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionModelId",
                table: "PrescriptionItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdType",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompleteTime",
                table: "MedicalCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MedicalCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "MedicalCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Herbs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "Herbs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastOperatorId",
                table: "Herbs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastOperatorName",
                table: "Herbs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Formulas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Formulas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "Formulas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentPrinciple",
                table: "Consultations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TCMDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "AllergyHistory",
                table: "Consultations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultationTime",
                table: "Consultations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Consultations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DiagnosisCatalogId",
                table: "Consultations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiastolicPressure",
                table: "Consultations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Consultations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeartRate",
                table: "Consultations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastHistory",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalExamination",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PulseCondition",
                table: "Consultations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespiratoryRate",
                table: "Consultations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystolicPressure",
                table: "Consultations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "Consultations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TongueInspection",
                table: "Consultations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WesternDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuthSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuthSessions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AuthSessions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AnomaliesDescription",
                table: "AuthSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                table: "AuthSessions",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "AuthSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "AuthSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtendedData",
                table: "AuthSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoLocation",
                table: "AuthSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAnomalies",
                table: "AuthSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoLogout",
                table: "AuthSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTokenRevoked",
                table: "AuthSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JwtTokenHash",
                table: "AuthSessions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityTime",
                table: "AuthSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshTime",
                table: "AuthSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoginType",
                table: "AuthSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "RefreshCount",
                table: "AuthSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "AuthSessions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokeReason",
                table: "AuthSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokeTime",
                table: "AuthSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedBy",
                table: "AuthSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerInfo",
                table: "AuthSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiryTime",
                table: "AuthSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "AuthSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AuthSessions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ComplianceFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IcdCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TcmSyndrome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisCatalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisCatalogs_DiagnosisCatalogs_ParentId",
                        column: x => x.ParentId,
                        principalTable: "DiagnosisCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BackupInterval = table.Column<int>(type: "int", nullable: false),
                    DefaultRecordSharing = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EnableAuditLog = table.Column<bool>(type: "bit", nullable: false),
                    EnablePerformanceMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogRetentionDays = table.Column<int>(type: "int", nullable: false),
                    MaxFileUploadSizeMB = table.Column<int>(type: "int", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    SyncMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SystemLogo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SystemVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfrastructureLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfrastructureLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdditionalData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttemptTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DetailedError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GeoLocationDetails = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoginType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessingNode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SecurityScore = table.Column<int>(type: "int", nullable: false),
                    ServerInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThreatIndicators = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserAgentParsed = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdditionalData = table.Column<string>(type: "text", nullable: true),
                    CacheHits = table.Column<int>(type: "int", nullable: true),
                    CacheMisses = table.Column<int>(type: "int", nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CpuUsage = table.Column<double>(type: "float", nullable: true),
                    DatabaseQueries = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    MemoryUsage = table.Column<long>(type: "bigint", nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PerformanceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestSize = table.Column<long>(type: "bigint", nullable: true),
                    ResponseSize = table.Column<long>(type: "bigint", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffectedResource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ArchivedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoAnalysisResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CategoryTags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ComplianceFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NotifiedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    RelatedEventsCount = table.Column<int>(type: "int", nullable: false),
                    RemediationSuggestions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresEscalation = table.Column<bool>(type: "bit", nullable: false),
                    RequiresNotification = table.Column<bool>(type: "bit", nullable: false),
                    ResponseData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetentionExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServerInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserActionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Function = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionModelId",
                table: "PrescriptionItems",
                column: "PrescriptionModelId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_ConsultationId",
                table: "MedicalCases",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_CreateTime",
                table: "MedicalCases",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ConsultationTime",
                table: "Consultations",
                column: "ConsultationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_Username",
                table: "AuthSessions",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceType",
                table: "AuditLogs",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCatalogs_Code",
                table: "DiagnosisCatalogs",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCatalogs_IsCommon",
                table: "DiagnosisCatalogs",
                column: "IsCommon");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCatalogs_IsEnabled",
                table: "DiagnosisCatalogs",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCatalogs_Name",
                table: "DiagnosisCatalogs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisCatalogs_ParentId",
                table: "DiagnosisCatalogs",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_IsResolved",
                table: "ErrorLogs",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_OccurredAt",
                table: "ErrorLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Severity",
                table: "ErrorLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_InfrastructureLogs_LogTime",
                table: "InfrastructureLogs",
                column: "LogTime");

            migrationBuilder.CreateIndex(
                name: "IX_InfrastructureLogs_LogType",
                table: "InfrastructureLogs",
                column: "LogType");

            migrationBuilder.CreateIndex(
                name: "IX_InfrastructureLogs_OperatorId",
                table: "InfrastructureLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptTime",
                table: "LoginAttempts",
                column: "AttemptTime");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_ClientIp",
                table: "LoginAttempts",
                column: "ClientIp");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsSuccess",
                table: "LoginAttempts",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_RiskLevel",
                table: "LoginAttempts",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Username",
                table: "LoginAttempts",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceLogs_Duration",
                table: "PerformanceLogs",
                column: "Duration");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceLogs_PerformanceLevel",
                table: "PerformanceLogs",
                column: "PerformanceLevel");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceLogs_StartTime",
                table: "PerformanceLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_EventTime",
                table: "SecurityLogs",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_EventType",
                table: "SecurityLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_IsProcessed",
                table: "SecurityLogs",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_Level",
                table: "SecurityLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_RequiresNotification",
                table: "SecurityLogs",
                column: "RequiresNotification");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityLogs_UserId",
                table: "SecurityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Group",
                table: "Settings",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_IsEnabled",
                table: "Settings",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Level",
                table: "SystemLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_LogTime",
                table: "SystemLogs",
                column: "LogTime");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Source",
                table: "SystemLogs",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_ActionTime",
                table: "UserActionLogs",
                column: "ActionTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_ActionType",
                table: "UserActionLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_UserId",
                table: "UserActionLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCases_Consultations_ConsultationId",
                table: "MedicalCases",
                column: "ConsultationId",
                principalTable: "Consultations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_Prescriptions_PrescriptionModelId",
                table: "PrescriptionItems",
                column: "PrescriptionModelId",
                principalTable: "Prescriptions",
                principalColumn: "Id");
        }
    }
}
