using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TaxLineIds : Migration
    {
        /// <summary>Each stored number of the removed tax line enum, and the line it becomes. 0 (none) becomes null.</summary>
        public static readonly IReadOnlyDictionary<int, string> LineIdByOldValue = new Dictionary<int, string>
        {
            [1] = "1120S-1A",
            [2] = "1120S-2",
            [7] = "1120S-7",
            [8] = "1120S-8",
            [9] = "1120S-9",
            [10] = "1120S-10",
            [11] = "1120S-11",
            [12] = "1120S-12",
            [13] = "1120S-13",
            [14] = "1120S-14",
            [15] = "1120S-15",
            [16] = "1120S-16",
            [17] = "1120S-17",
            [18] = "1120S-18",
            [19] = "1120S-20",
            [20] = "1120S-20",
            [21] = "1120S-20",
            [101] = "SCHL-1",
            [102] = "SCHL-2A",
            [103] = "SCHL-3",
            [106] = "SCHL-6",
            [107] = "SCHL-7",
            [108] = "SCHL-8",
            [109] = "SCHL-9",
            [110] = "SCHL-10A",
            [112] = "SCHL-13A",
            [114] = "SCHL-14",
            [116] = "SCHL-16",
            [117] = "SCHL-17",
            [118] = "SCHL-18",
            [119] = "SCHL-19",
            [120] = "SCHL-20",
            [121] = "SCHL-21",
            [122] = "SCHL-22",
            [123] = "SCHL-23",
            [124] = "SCHL-24",
            [200] = "SCHK-16D"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMeals",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxLineId",
                table: "Accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            var toLine = string.Join(" ", LineIdByOldValue.Select(p => $"WHEN {p.Key} THEN '{p.Value}'"));
            migrationBuilder.Sql($@"UPDATE ""Accounts"" SET ""TaxLineId"" = CASE ""TaxFormLine"" {toLine} ELSE NULL END;");

            migrationBuilder.DropColumn(
                name: "TaxFormLine",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "BookSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Chart = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxYear = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MileageLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Miles = table.Column<decimal>(type: "numeric(10,1)", nullable: false),
                    StartOdometer = table.Column<decimal>(type: "numeric(10,1)", nullable: false),
                    EndOdometer = table.Column<decimal>(type: "numeric(10,1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MileageLog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookSettings");

            migrationBuilder.DropTable(
                name: "MileageLog");

            migrationBuilder.DropColumn(
                name: "IsMeals",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "TaxFormLine",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Travel and meals shared line 20 with other deductions, so those come back as other deductions (19).
            var toValue = string.Join(" ", LineIdByOldValue.GroupBy(p => p.Value)
                .Select(g => $"WHEN '{g.Key}' THEN {g.Min(p => p.Key)}"));
            migrationBuilder.Sql($@"UPDATE ""Accounts"" SET ""TaxFormLine"" = CASE ""TaxLineId"" {toValue} ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "TaxLineId",
                table: "Accounts");
        }
    }
}
