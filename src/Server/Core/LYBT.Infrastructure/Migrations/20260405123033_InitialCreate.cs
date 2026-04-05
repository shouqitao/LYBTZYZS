using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formulas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Indication = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Usage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Property = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ValidationStatus = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FormulaType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formulas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Herbs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinYinCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Origin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Spec = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Effect = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Usage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Herbs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinYinCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    MaritalStatus = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdType = table.Column<int>(type: "int", nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AllergyHistory = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MedicalHistory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BloodType = table.Column<int>(type: "int", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisableReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastVisitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisitCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoggerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RealName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PinYinCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangeOnNextLogin = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormulaHerbItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormulaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalHerbName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsValidated = table.Column<bool>(type: "bit", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dosage = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "g"),
                    Usage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProcessingMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecocteMethod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaHerbItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaHerbItems_Formulas_FormulaId",
                        column: x => x.FormulaId,
                        principalTable: "Formulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormulaHerbItems_Herbs_HerbId",
                        column: x => x.HerbId,
                        principalTable: "Herbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutoLoginTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FamilyId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoLoginTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoLoginTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CaseStatus = table.Column<int>(type: "int", nullable: false),
                    NeedsPrescription = table.Column<bool>(type: "bit", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrintVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LastPrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCases_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalCases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Jti = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AbsoluteExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FamilyId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresentIllness = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TongueDiagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PulseDiagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TcmDiagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultations_MedicalCases_Id",
                        column: x => x.Id,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCaseAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OperatorRole = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCaseAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCaseAuditLogs_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCasePrintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintType = table.Column<int>(type: "int", nullable: false),
                    PrintVersion = table.Column<int>(type: "int", nullable: false),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrintedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrintedByName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrinterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCasePrintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCasePrintLogs_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DosageCount = table.Column<int>(type: "int", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Advice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferencedFormulas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dosage = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DecocteMethod = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoLoginTokens_FamilyId",
                table: "AutoLoginTokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoLoginTokens_Token",
                table: "AutoLoginTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoLoginTokens_UserId_UserName",
                table: "AutoLoginTokens",
                columns: new[] { "UserId", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaHerbItems_FormulaId",
                table: "FormulaHerbItems",
                column: "FormulaId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaHerbItems_HerbId",
                table: "FormulaHerbItems",
                column: "HerbId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt",
                table: "MedicalCaseAuditLogs",
                columns: new[] { "MedicalCaseId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCaseAuditLogs_OperatorId_CreatedAt",
                table: "MedicalCaseAuditLogs",
                columns: new[] { "OperatorId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCasePrintLogs_MedicalCaseId",
                table: "MedicalCasePrintLogs",
                column: "MedicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_UserId",
                table: "MedicalCases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_MedicalCases_Patient_ActiveOnly",
                table: "MedicalCases",
                column: "PatientId",
                unique: true,
                filter: "[CaseStatus] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Phone",
                table: "Patients",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "UX_Prescriptions_MedicalCaseId",
                table: "Prescriptions",
                column: "MedicalCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyId",
                table: "RefreshTokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked_Token",
                table: "RefreshTokens",
                columns: new[] { "IsRevoked", "Token" })
                .Annotation("SqlServer:Include", new[] { "UserId", "UserType", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_DoctorId",
                table: "Registrations",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_MedicalCaseId",
                table: "Registrations",
                column: "MedicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_PatientId",
                table: "Registrations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_Status",
                table: "Registrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "EventType", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_UserId_CreatedAt",
                table: "SecurityAuditLogs",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_CorrelationId",
                table: "SystemLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Level",
                table: "SystemLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Timestamp",
                table: "SystemLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_UserId",
                table: "SystemLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_Phone",
                table: "Users",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropTable(
                name: "AutoLoginTokens");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "FormulaHerbItems");

            migrationBuilder.DropTable(
                name: "MedicalCaseAuditLogs");

            migrationBuilder.DropTable(
                name: "MedicalCasePrintLogs");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "Formulas");

            migrationBuilder.DropTable(
                name: "Herbs");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "MedicalCases");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
