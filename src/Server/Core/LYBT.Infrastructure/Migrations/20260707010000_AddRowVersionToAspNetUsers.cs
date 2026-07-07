using Microsoft.EntityFrameworkCore.Migrations;

namespace LYBT.Infrastructure.Migrations;

public partial class AddRowVersionToAspNetUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "AspNetUsers",
            type: "rowversion",
            rowVersion: true,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "AspNetUsers");
    }
}
