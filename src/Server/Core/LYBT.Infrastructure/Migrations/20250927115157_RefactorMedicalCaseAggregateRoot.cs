using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMedicalCaseAggregateRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "BlacklistedTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_IsUsed_IsRevoked_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_PatientId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "IX_Consultations_UserId",
                table: "Consultations");

            migrationBuilder.DropIndex(
                name: "UX_Consultations_MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "MedicalCaseId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Consultations");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                table: "RefreshTokens",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "JwtId",
                table: "RefreshTokens",
                newName: "Jti");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "RefreshTokens",
                newName: "ClientIp");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_JwtId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_Jti");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "RevokedReason",
                table: "RefreshTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "RefreshTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplacedByToken",
                table: "RefreshTokens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy",
                table: "RefreshTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "TCMDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

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
                name: "IX_User_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_MedicalCase_Status",
                table: "Prescriptions",
                columns: new[] { "MedicalCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Patient_Date",
                table: "Prescriptions",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Status",
                table: "Prescriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_CreatedAt",
                table: "Patients",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_IdNumber",
                table: "Patients",
                column: "IdNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Name_Phone",
                table: "Patients",
                columns: new[] { "Name", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Phone",
                table: "Patients",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_PinYin_Deleted",
                table: "Patients",
                columns: new[] { "PinYinCode", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCase_Doctor_Status",
                table: "MedicalCases",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCase_Patient_Date",
                table: "MedicalCases",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCase_Status_Date",
                table: "MedicalCases",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Consultations_MedicalCases_Id",
                table: "Consultations",
                column: "Id",
                principalTable: "MedicalCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultations_MedicalCases_Id",
                table: "Consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_User_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_User_Phone",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_User_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Prescription_MedicalCase_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescription_Patient_Date",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescription_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Patient_CreatedAt",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patient_IdNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patient_Name_Phone",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patient_Phone",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patient_PinYin_Deleted",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCase_Doctor_Status",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCase_Patient_Date",
                table: "MedicalCases");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCase_Status_Date",
                table: "MedicalCases");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ReplacedByToken",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                table: "RefreshTokens",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "Jti",
                table: "RefreshTokens",
                newName: "JwtId");

            migrationBuilder.RenameColumn(
                name: "ClientIp",
                table: "RefreshTokens",
                newName: "IpAddress");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_Jti",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_JwtId");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "RevokedReason",
                table: "RefreshTokens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TCMDiagnosis",
                table: "Consultations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalCaseId",
                table: "Consultations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "Consultations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Consultations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "BlacklistedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlacklistedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RevokedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsUsed_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsUsed", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_PatientId",
                table: "Consultations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_UserId",
                table: "Consultations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Consultations_MedicalCaseId",
                table: "Consultations",
                column: "MedicalCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_BlacklistedAt",
                table: "BlacklistedTokens",
                column: "BlacklistedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_JwtId",
                table: "BlacklistedTokens",
                column: "JwtId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_TokenExpiresAt",
                table: "BlacklistedTokens",
                column: "TokenExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_UserId",
                table: "BlacklistedTokens",
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
    }
}
