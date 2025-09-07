using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{

    /// <inheritdoc />
    public partial class Auth_UltraThink_Refactor : Migration
    {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Consultations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JwtTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTokenRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokeReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RevokeTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefreshCount = table.Column<int>(type: "int", nullable: false),
                    LastRefreshTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExtendedData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ServerInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GeoLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsAutoLogout = table.Column<bool>(type: "bit", nullable: false),
                    HasAnomalies = table.Column<bool>(type: "bit", nullable: false),
                    AnomaliesDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoginType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    RememberMe = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerInfo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    ProcessingNode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DetailedError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecurityScore = table.Column<int>(type: "int", nullable: false),
                    GeoLocationDetails = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ThreatIndicators = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserAgentParsed = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LoginType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    NotifiedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEventsCount = table.Column<int>(type: "int", nullable: false),
                    CategoryTags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    AutoAnalysisResult = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RemediationSuggestions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiresEscalation = table.Column<bool>(type: "bit", nullable: false),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false),
                    ComplianceFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RetentionExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AffectedResource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiresNotification = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_LoginTime",
                table: "AuthSessions",
                column: "LoginTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_Status",
                table: "AuthSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_UserId",
                table: "AuthSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_Username",
                table: "AuthSessions",
                column: "Username");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_Patients_PatientId",
                table: "Consultations",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_Users_UserId",
                table: "Consultations",
                column: "UserId",
                principalTable: "Users",
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
                name: "FK_Consultations_Patients_PatientId",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_Users_UserId",
                table: "Consultations");

            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "SecurityLogs");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Consultations");
        }
    }
}
