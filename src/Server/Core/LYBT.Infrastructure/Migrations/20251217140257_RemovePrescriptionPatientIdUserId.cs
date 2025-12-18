using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrescriptionPatientIdUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除依赖PatientId的索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prescription_Patient_Date' AND object_id = OBJECT_ID('Prescriptions'))
                BEGIN
                    DROP INDEX [IX_Prescription_Patient_Date] ON [Prescriptions];
                END
            ");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Prescriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: true);

            // 恢复索引
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_Prescription_Patient_Date] ON [Prescriptions] ([PatientId], [CreatedAt]);
            ");
        }
    }
}
