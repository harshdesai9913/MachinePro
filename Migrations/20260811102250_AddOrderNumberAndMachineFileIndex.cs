using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MachinePro.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberAndMachineFileIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MachineBuildNumber",
                table: "Jobs",
                newName: "OrderNumber");

            migrationBuilder.RenameColumn(
                name: "MachineBuildNumber",
                table: "CapacityLedgerEntries",
                newName: "OrderNumber");

            migrationBuilder.CreateTable(
                name: "MachineFileEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    Customer = table.Column<string>(type: "text", nullable: false),
                    ModelNo = table.Column<string>(type: "text", nullable: false),
                    DrawingNo = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    UploadedDate = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineFileEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineFileEntries");

            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "Jobs",
                newName: "MachineBuildNumber");

            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "CapacityLedgerEntries",
                newName: "MachineBuildNumber");
        }
    }
}
