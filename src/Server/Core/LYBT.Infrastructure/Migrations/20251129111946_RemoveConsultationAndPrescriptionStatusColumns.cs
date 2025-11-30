using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <summary>
    /// 移除Consultation和Prescription的Status列
    /// DD-002: Status从聚合根MedicalCase派生
    /// </summary>
    public partial class RemoveConsultationAndPrescriptionStatusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除依赖Status列的索引
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Prescriptions') AND name = 'IX_Prescription_Status')
                BEGIN
                    DROP INDEX [IX_Prescription_Status] ON [Prescriptions];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Prescriptions') AND name = 'IX_Prescription_MedicalCase_Status')
                BEGIN
                    DROP INDEX [IX_Prescription_MedicalCase_Status] ON [Prescriptions];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Consultations') AND name = 'IX_Consultation_Status')
                BEGIN
                    DROP INDEX [IX_Consultation_Status] ON [Consultations];
                END
            ");

            // 再删除列
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Consultations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Consultations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
