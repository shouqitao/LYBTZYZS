using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.TreatmentRoom.Migrations
{
    /// <inheritdoc />
    public partial class InitialTreatmentRoomMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_DoctorId",
                table: "TreatmentRooms",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_PatientId",
                table: "TreatmentRooms",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_Status",
                table: "TreatmentRooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRooms_TreatmentType",
                table: "TreatmentRooms",
                column: "TreatmentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreatmentRooms");
        }
    }
}
