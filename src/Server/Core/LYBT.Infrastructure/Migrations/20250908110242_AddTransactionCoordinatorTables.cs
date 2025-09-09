using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCoordinatorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokenStore");

            migrationBuilder.DropTable(
                name: "SuspiciousTokenActivity");

            migrationBuilder.DropTable(
                name: "TokenStore");

            migrationBuilder.CreateTable(
                name: "TransactionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStepLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompensation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStepLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStepLogs_TransactionLogs_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TransactionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_StartTime",
                table: "TransactionLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_Status",
                table: "TransactionLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_TransactionId",
                table: "TransactionLogs",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_UserId",
                table: "TransactionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStepLogs_StartTime",
                table: "TransactionStepLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStepLogs_Status",
                table: "TransactionStepLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStepLogs_TransactionId",
                table: "TransactionStepLogs",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStepLogs_TransactionId_StepOrder",
                table: "TransactionStepLogs",
                columns: new[] { "TransactionId", "StepOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionStepLogs");

            migrationBuilder.DropTable(
                name: "TransactionLogs");

            migrationBuilder.CreateTable(
                name: "RefreshTokenStore",
                columns: table => new
                {
                    RefreshToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccessTokenId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLongTerm = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    RevokeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
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
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    HandledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandledNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsHandled = table.Column<bool>(type: "bit", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "Medium"),
                    TokenId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    ClientIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "access_token"),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenStore", x => x.TokenId);
                });

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
        }
    }
}
