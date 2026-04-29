using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MachinePro.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapacityLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntryDate = table.Column<string>(type: "text", nullable: false),
                    Serial = table.Column<string>(type: "text", nullable: false),
                    Customer = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false),
                    MachineNumber = table.Column<string>(type: "text", nullable: true),
                    MachineBuildNumber = table.Column<string>(type: "text", nullable: true),
                    QtyProduced = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    EnteredBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapacityLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Serial = table.Column<string>(type: "text", nullable: false),
                    Customer = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Drawing = table.Column<string>(type: "text", nullable: false),
                    ItemCode = table.Column<string>(type: "text", nullable: true),
                    MachineBuildNumber = table.Column<string>(type: "text", nullable: true),
                    DrawingDescription = table.Column<string>(type: "text", nullable: true),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    InwardDate = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedDate = table.Column<string>(type: "text", nullable: true),
                    Process1 = table.Column<string>(type: "text", nullable: true),
                    Process2 = table.Column<string>(type: "text", nullable: true),
                    Process3 = table.Column<string>(type: "text", nullable: true),
                    Process4 = table.Column<string>(type: "text", nullable: true),
                    Process5 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModelName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlannerHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Serial = table.Column<string>(type: "text", nullable: false),
                    Customer = table.Column<string>(type: "text", nullable: false),
                    Step = table.Column<string>(type: "text", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: false),
                    NewValue = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false),
                    MachineNumber = table.Column<string>(type: "text", nullable: true),
                    MachineBuildNumber = table.Column<string>(type: "text", nullable: true),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    FinishedQty = table.Column<int>(type: "integer", nullable: true),
                    FinishedDate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleEntries_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "FullName", "Password", "Role", "Username" },
                values: new object[] { 1, "Administrator", "admin", "Manager", "admin" });

            migrationBuilder.InsertData(
                table: "CustomerMasters",
                columns: new[] { "Id", "City", "Country", "CustomerName", "State" },
                values: new object[,]
                {
                    { 1, null, "India", "Tata Motors", null },
                    { 2, null, "India", "Reliance Ind", null },
                    { 3, null, "India", "L&T Heavy", null },
                    { 4, null, "India", "Bajaj Auto", null },
                    { 5, null, "India", "Mahindra & Mahindra", null }
                });

            migrationBuilder.InsertData(
                table: "Jobs",
                columns: new[] { "Id", "CompletedDate", "Customer", "Drawing", "DrawingDescription", "InwardDate", "IsCompleted", "ItemCode", "MachineBuildNumber", "Model", "Priority", "Process1", "Process2", "Process3", "Process4", "Process5", "Qty", "Serial" },
                values: new object[,]
                {
                    { 1, null, "Tata Motors", "DWG-2201", "Engine block housing plate", "24/03/2026", false, null, null, "TM-400X", null, "VMC", "Milling", "Lathe", null, null, 12, "SN-001" },
                    { 2, null, "Reliance Ind", "DWG-3302", "Pump shaft coupling", "24/03/2026", false, null, null, "RI-750", null, "Lathe", "Shaper", null, null, null, 8, "SN-002" }
                });

            migrationBuilder.InsertData(
                table: "ModelMasters",
                columns: new[] { "Id", "ModelName" },
                values: new object[,]
                {
                    { 1, "TM-400X" },
                    { 2, "TM-500Y" },
                    { 3, "RI-750" },
                    { 4, "LT-900K" },
                    { 5, "BA-200Z" },
                    { 6, "MM-600A" }
                });

            migrationBuilder.InsertData(
                table: "ModuleEntries",
                columns: new[] { "Id", "FinishedDate", "FinishedQty", "IsFinished", "JobId", "MachineBuildNumber", "MachineNumber", "ModuleName" },
                values: new object[,]
                {
                    { 1, null, null, false, 1, null, null, "VMC" },
                    { 2, null, null, false, 1, null, null, "Milling" },
                    { 3, null, null, false, 1, null, null, "Lathe" },
                    { 4, null, null, false, 2, null, null, "Lathe" },
                    { 5, null, null, false, 2, null, null, "Shaper" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleEntries_JobId",
                table: "ModuleEntries",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "CapacityLedgerEntries");

            migrationBuilder.DropTable(
                name: "CustomerMasters");

            migrationBuilder.DropTable(
                name: "ModelMasters");

            migrationBuilder.DropTable(
                name: "ModuleEntries");

            migrationBuilder.DropTable(
                name: "PlannerHistories");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
