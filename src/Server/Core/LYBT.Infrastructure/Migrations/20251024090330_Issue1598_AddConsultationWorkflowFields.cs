using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Issue1598_AddConsultationWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrescriptionEnabled",
                table: "Consultations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Step1CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Step2CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Step3CompletedAt",
                table: "Consultations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrescriptionEnabled",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Step1CompletedAt",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Step2CompletedAt",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "Step3CompletedAt",
                table: "Consultations");
        }
    }
}
