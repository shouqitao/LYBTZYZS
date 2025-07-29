using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedTypesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Users",
                newName: "CreatedTime");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinyinCode",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WuBiCode",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdminSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSecrets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ComplianceFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingItems",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingItems", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "Billings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BillingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IcdCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TcmSyndrome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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
                name: "DiagnosisTreatments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiagnosisCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Formula_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisTreatments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<int>(type: "int", nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WorkStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WuBiCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormulaTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Usage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Property = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SharedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SystemVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SystemLogo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DefaultRecordSharing = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SyncMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BackupInterval = table.Column<int>(type: "int", nullable: false),
                    LogRetentionDays = table.Column<int>(type: "int", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxFileUploadSizeMB = table.Column<int>(type: "int", nullable: false),
                    EnableAuditLog = table.Column<bool>(type: "bit", nullable: false),
                    EnablePerformanceMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    LogType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfrastructureLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PinyinCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WuBiCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisableReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    CpuUsage = table.Column<double>(type: "float", nullable: true),
                    MemoryUsage = table.Column<long>(type: "bigint", nullable: true),
                    DatabaseQueries = table.Column<int>(type: "int", nullable: true),
                    CacheHits = table.Column<int>(type: "int", nullable: true),
                    CacheMisses = table.Column<int>(type: "int", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    RequestSize = table.Column<long>(type: "bigint", nullable: true),
                    ResponseSize = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformanceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AdditionalData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NeedDecoction = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispenseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DosageCount = table.Column<int>(type: "int", nullable: false),
                    SingleDosePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    Advice = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FormulaSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DuplicateWarning = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MissingDrugWarning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Queueings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueueType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueueTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queueings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiagnosisText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrescriptionSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TreatmentSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    VisitTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TreatmentAdvice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiagnosisResults = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharedToDoctorIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistrationType = table.Column<int>(type: "int", nullable: false),
                    IsFromDoctor = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegistrationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecuteTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServerInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    Indications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Contraindications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Precautions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequireAppointment = table.Column<bool>(type: "bit", nullable: false),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCatalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCatalogs_TreatmentCatalogs_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TreatmentCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TreatmentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExecutedCount = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Executor = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastExecuteTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TreatmentItem = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserActionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Function = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisTreatments_Herbs",
                columns: table => new
                {
                    FormulaModelDiagnosisTreatmentModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisTreatments_Herbs", x => new { x.FormulaModelDiagnosisTreatmentModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_DiagnosisTreatments_Herbs_DiagnosisTreatments_FormulaModelDiagnosisTreatmentModelId",
                        column: x => x.FormulaModelDiagnosisTreatmentModelId,
                        principalTable: "DiagnosisTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisTreatments_Treatments",
                columns: table => new
                {
                    DiagnosisTreatmentModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisTreatments_Treatments", x => new { x.DiagnosisTreatmentModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_DiagnosisTreatments_Treatments_DiagnosisTreatments_DiagnosisTreatmentModelId",
                        column: x => x.DiagnosisTreatmentModelId,
                        principalTable: "DiagnosisTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Herbs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Specification = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PinyinCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WuBiCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Effect = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastOperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PharmacyModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Herbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Herbs_Pharmacies_PharmacyModelId",
                        column: x => x.PharmacyModelId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Usage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrescriptionModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionModelId",
                        column: x => x.PrescriptionModelId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Records_HerbalFormula",
                columns: table => new
                {
                    RecordModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records_HerbalFormula", x => new { x.RecordModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_Records_HerbalFormula_Records_RecordModelId",
                        column: x => x.RecordModelId,
                        principalTable: "Records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Records_TreatmentPlans",
                columns: table => new
                {
                    RecordModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records_TreatmentPlans", x => new { x.RecordModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_Records_TreatmentPlans_Records_RecordModelId",
                        column: x => x.RecordModelId,
                        principalTable: "Records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId");

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
                name: "IX_Herbs_ExpireDate",
                table: "Herbs",
                column: "ExpireDate");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_Name",
                table: "Herbs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_PharmacyModelId",
                table: "Herbs",
                column: "PharmacyModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_PinyinCode",
                table: "Herbs",
                column: "PinyinCode");

            migrationBuilder.CreateIndex(
                name: "IX_Herbs_WuBiCode",
                table: "Herbs",
                column: "WuBiCode");

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
                name: "IX_PrescriptionItems_PrescriptionModelId",
                table: "PrescriptionItems",
                column: "PrescriptionModelId");

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
                name: "IX_TreatmentCatalogs_Code",
                table: "TreatmentCatalogs",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_IsCommon",
                table: "TreatmentCatalogs",
                column: "IsCommon");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_IsEnabled",
                table: "TreatmentCatalogs",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_Name",
                table: "TreatmentCatalogs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_ParentId",
                table: "TreatmentCatalogs",
                column: "ParentId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminSecrets");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BillingItems");

            migrationBuilder.DropTable(
                name: "Billings");

            migrationBuilder.DropTable(
                name: "DiagnosisCatalogs");

            migrationBuilder.DropTable(
                name: "DiagnosisTreatments_Herbs");

            migrationBuilder.DropTable(
                name: "DiagnosisTreatments_Treatments");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "FormulaTemplates");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "Herbs");

            migrationBuilder.DropTable(
                name: "InfrastructureLogs");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "PerformanceLogs");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "Queueings");

            migrationBuilder.DropTable(
                name: "Records_HerbalFormula");

            migrationBuilder.DropTable(
                name: "Records_TreatmentPlans");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "SyncTasks");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "TreatmentCatalogs");

            migrationBuilder.DropTable(
                name: "TreatmentRooms");

            migrationBuilder.DropTable(
                name: "UserActionLogs");

            migrationBuilder.DropTable(
                name: "DiagnosisTreatments");

            migrationBuilder.DropTable(
                name: "Pharmacies");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "Records");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PinyinCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WuBiCode",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "CreatedTime",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
