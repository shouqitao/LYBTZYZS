using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Prescriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Patients",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MedicalCases",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Herbs",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Formulas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Formulas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Formulas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FormulaType",
                table: "Formulas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Formulas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Formulas",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Formulas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Formulas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Formulas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Consultations",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Jti = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

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
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "FormulaType",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Formulas");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Formulas");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Prescriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Patients",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MedicalCases",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Herbs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Consultations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);
        }
    }
}
