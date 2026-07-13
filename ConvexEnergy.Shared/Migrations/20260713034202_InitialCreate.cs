using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvexEnergy.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DayAheadPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeliveryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Period = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceEs = table.Column<decimal>(type: "TEXT", nullable: false),
                    PricePt = table.Column<decimal>(type: "TEXT", nullable: false),
                    FileVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayAheadPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DayAheadPrices_DeliveryDate_Period",
                table: "DayAheadPrices",
                columns: new[] { "DeliveryDate", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DayAheadPrices");
        }
    }
}
