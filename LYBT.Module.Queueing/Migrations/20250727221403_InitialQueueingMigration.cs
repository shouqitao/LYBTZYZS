using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LYBT.Module.Queueing.Migrations {

    /// <inheritdoc />
    public partial class InitialQueueingMigration : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "Queueings",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueueType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QueueTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Queueings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Queueings_DoctorId",
                table: "Queueings",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Queueings_PatientId",
                table: "Queueings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Queueings_QueueTime",
                table: "Queueings",
                column: "QueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_Queueings_QueueType",
                table: "Queueings",
                column: "QueueType");

            migrationBuilder.CreateIndex(
                name: "IX_Queueings_Status",
                table: "Queueings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "Queueings");
        }
    }
}