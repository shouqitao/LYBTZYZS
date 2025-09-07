using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{

    /// <inheritdoc />
    public partial class ExtendConsultationModel : Migration
    {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentCatalogs_TreatmentCatalogs_ParentId",
                table: "TreatmentCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCatalogs_IsCommon",
                table: "TreatmentCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCatalogs_IsEnabled",
                table: "TreatmentCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCatalogs_ParentId",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "ExecutedCount",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "Executor",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "TotalCount",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "BillingTime",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BillingItems");

            migrationBuilder.RenameColumn(
                name: "TreatmentType",
                table: "TreatmentRooms",
                newName: "RoomNumber");

            migrationBuilder.RenameColumn(
                name: "TreatmentItem",
                table: "TreatmentRooms",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "TreatmentRooms",
                newName: "UpdateTime");

            migrationBuilder.RenameColumn(
                name: "LastExecuteTime",
                table: "TreatmentRooms",
                newName: "CreateTime");

            migrationBuilder.RenameColumn(
                name: "RequireAppointment",
                table: "TreatmentCatalogs",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "Pharmacies",
                newName: "MedicalCaseId");

            migrationBuilder.RenameColumn(
                name: "NeedDecoction",
                table: "Pharmacies",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "BillingId",
                table: "Billings",
                newName: "BillingNumber");

            migrationBuilder.RenameColumn(
                name: "CompletedTime",
                table: "Billings",
                newName: "UpdateTime");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "TreatmentRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleDoctorId",
                table: "TreatmentRooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleDoctorName",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdateTime",
                table: "TreatmentCatalogs",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Precautions",
                table: "TreatmentCatalogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "TreatmentCatalogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "TreatmentCatalogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TreatmentCatalogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TreatmentCatalogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TreatmentCatalogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormulaId",
                table: "Records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentRoomIds",
                table: "Records",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "PharmacyHerbs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "PharmacyHerbs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "PharmacyHerbs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Pharmacies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispensingTime",
                table: "Pharmacies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PharmacistId",
                table: "Pharmacies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "Pharmacies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhone",
                table: "Pharmacies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "Pharmacies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Billings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefundReason",
                table: "Billings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Billings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BillingNumber",
                table: "Billings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "CashierId",
                table: "Billings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteTime",
                table: "Billings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Billings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Billings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInvoiced",
                table: "Billings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordId",
                table: "Billings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Billings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundOperatorId",
                table: "Billings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegistrationId",
                table: "Billings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingId1",
                table: "BillingItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingModelId",
                table: "BillingItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "BillingItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "BillingItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "BillingItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "BillingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "BillingItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "BillingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedId",
                table: "BillingItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "BillingItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BillingItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "BillingItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "BillingItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "BillingItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PresentIllness = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PastHistory = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AllergyHistory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhysicalExamination = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Inspection = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuscultationOlfaction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Inquiry = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Palpation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TongueInspection = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PulseCondition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SystolicPressure = table.Column<int>(type: "int", nullable: true),
                    DiastolicPressure = table.Column<int>(type: "int", nullable: true),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    TCMDiagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WesternDiagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DiagnosisCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreatmentPrinciple = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MedicalAdvice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConsultationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Prescription_DosageCount = table.Column<int>(type: "int", nullable: true),
                    Prescription_Instructions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Prescription_SpecialInstructions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrescriptionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhysiotherapyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapistId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutedCount = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Executor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastExecuteTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentItem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreatmentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreatmentRoomServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCases_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalCases_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalCases_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhysiotherapyItemModel",
                columns: table => new
                {
                    TreatmentPlanModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TreatmentArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Count = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysiotherapyItemModel", x => new { x.TreatmentPlanModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_PhysiotherapyItemModel_TreatmentPlans_TreatmentPlanModelId",
                        column: x => x.TreatmentPlanModelId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPrescriptionHerbModel",
                columns: table => new
                {
                    TreatmentPrescriptionModelTreatmentPlanModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HerbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpecialUsage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPrescriptionHerbModel", x => new { x.TreatmentPrescriptionModelTreatmentPlanModelId, x.Id });
                    table.ForeignKey(
                        name: "FK_TreatmentPrescriptionHerbModel_TreatmentPlans_TreatmentPrescriptionModelTreatmentPlanModelId",
                        column: x => x.TreatmentPrescriptionModelTreatmentPlanModelId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_Category",
                table: "TreatmentCatalogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_IsActive",
                table: "TreatmentCatalogs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_MedicalCaseId",
                table: "Pharmacies",
                column: "MedicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_PrescriptionId",
                table: "Pharmacies",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_CashierId",
                table: "Billings",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_DoctorId",
                table: "Billings",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_PatientId",
                table: "Billings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_PrescriptionId",
                table: "Billings",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RecordId",
                table: "Billings",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RefundOperatorId",
                table: "Billings",
                column: "RefundOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Billings_RegistrationId",
                table: "Billings",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingItems_BillingId1",
                table: "BillingItems",
                column: "BillingId1");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_ConsultationTime",
                table: "Consultations",
                column: "ConsultationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_DoctorId",
                table: "Consultations",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_PatientId",
                table: "Consultations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_ConsultationId",
                table: "MedicalCases",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_CreateTime",
                table: "MedicalCases",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_DoctorId",
                table: "MedicalCases",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PatientId",
                table: "MedicalCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_RegistrationId",
                table: "MedicalCases",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_Status",
                table: "MedicalCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_TreatmentPlanId",
                table: "MedicalCases",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_ConsultationId",
                table: "TreatmentPlans",
                column: "ConsultationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingItems_Billings_BillingId1",
                table: "BillingItems",
                column: "BillingId1",
                principalTable: "Billings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Doctors_DoctorId",
                table: "Billings",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Patients_PatientId",
                table: "Billings",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Prescriptions_PrescriptionId",
                table: "Billings",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Records_RecordId",
                table: "Billings",
                column: "RecordId",
                principalTable: "Records",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Registrations_RegistrationId",
                table: "Billings",
                column: "RegistrationId",
                principalTable: "Registrations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Users_CashierId",
                table: "Billings",
                column: "CashierId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Billings_Users_RefundOperatorId",
                table: "Billings",
                column: "RefundOperatorId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_MedicalCases_MedicalCaseId",
                table: "Pharmacies",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_Prescriptions_PrescriptionId",
                table: "Pharmacies",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingItems_Billings_BillingId1",
                table: "BillingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Doctors_DoctorId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Patients_PatientId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Prescriptions_PrescriptionId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Records_RecordId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Registrations_RegistrationId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Users_CashierId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Billings_Users_RefundOperatorId",
                table: "Billings");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_MedicalCases_MedicalCaseId",
                table: "Pharmacies");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_Prescriptions_PrescriptionId",
                table: "Pharmacies");

            migrationBuilder.DropTable(
                name: "MedicalCases");

            migrationBuilder.DropTable(
                name: "PhysiotherapyItemModel");

            migrationBuilder.DropTable(
                name: "TreatmentPrescriptionHerbModel");

            migrationBuilder.DropTable(
                name: "TreatmentTasks");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCatalogs_Category",
                table: "TreatmentCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCatalogs_IsActive",
                table: "TreatmentCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_MedicalCaseId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_PrescriptionId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Billings_CashierId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_DoctorId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_PatientId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_PrescriptionId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_RecordId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_RefundOperatorId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_Billings_RegistrationId",
                table: "Billings");

            migrationBuilder.DropIndex(
                name: "IX_BillingItems_BillingId1",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "ResponsibleDoctorId",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "ResponsibleDoctorName",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "TreatmentRooms");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TreatmentCatalogs");

            migrationBuilder.DropColumn(
                name: "FormulaId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "TreatmentRoomIds",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PharmacyHerbs");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "PharmacyHerbs");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "PharmacyHerbs");

            migrationBuilder.DropColumn(
                name: "DispensingTime",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "PharmacistId",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "ReceiverPhone",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "Pharmacies");

            migrationBuilder.DropColumn(
                name: "CashierId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "DeleteTime",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "IsInvoiced",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RefundOperatorId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "RegistrationId",
                table: "Billings");

            migrationBuilder.DropColumn(
                name: "BillingId1",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "BillingModelId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "RelatedId",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "Specification",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "BillingItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "BillingItems");

            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "TreatmentRooms",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "RoomNumber",
                table: "TreatmentRooms",
                newName: "TreatmentType");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "TreatmentRooms",
                newName: "TreatmentItem");

            migrationBuilder.RenameColumn(
                name: "CreateTime",
                table: "TreatmentRooms",
                newName: "LastExecuteTime");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "TreatmentCatalogs",
                newName: "RequireAppointment");

            migrationBuilder.RenameColumn(
                name: "MedicalCaseId",
                table: "Pharmacies",
                newName: "TaskId");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Pharmacies",
                newName: "NeedDecoction");

            migrationBuilder.RenameColumn(
                name: "BillingNumber",
                table: "Billings",
                newName: "BillingId");

            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "Billings",
                newName: "CompletedTime");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Count",
                table: "TreatmentRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DoctorId",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "TreatmentRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ExecutedCount",
                table: "TreatmentRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionId",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Executor",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "PatientId",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                table: "TreatmentRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "TotalCount",
                table: "TreatmentRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdateTime",
                table: "TreatmentCatalogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Precautions",
                table: "TreatmentCatalogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "TreatmentCatalogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "TreatmentCatalogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "TreatmentCatalogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "TreatmentCatalogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "TreatmentCatalogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "TreatmentCatalogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "TreatmentCatalogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Pharmacies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "Pharmacies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                table: "Pharmacies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "Billings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefundReason",
                table: "Billings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Billings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingId",
                table: "Billings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingTime",
                table: "Billings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BillingItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_IsCommon",
                table: "TreatmentCatalogs",
                column: "IsCommon");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_IsEnabled",
                table: "TreatmentCatalogs",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCatalogs_ParentId",
                table: "TreatmentCatalogs",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentCatalogs_TreatmentCatalogs_ParentId",
                table: "TreatmentCatalogs",
                column: "ParentId",
                principalTable: "TreatmentCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
