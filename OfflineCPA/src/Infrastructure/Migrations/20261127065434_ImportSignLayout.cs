using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImportSignLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BalanceCheckFailed",
                table: "StagedRows",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BalanceCheck",
                table: "ImportBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BalanceReason",
                table: "ImportBatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmountLayout",
                table: "CsvMappingProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DebitsCarryMinus",
                table: "CsvMappingProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorColumn",
                table: "CsvMappingProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LayoutConfirmed",
                table: "CsvMappingProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MoneyInMarker",
                table: "CsvMappingProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoneyOutMarker",
                table: "CsvMappingProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceCheckFailed",
                table: "StagedRows");

            migrationBuilder.DropColumn(
                name: "BalanceCheck",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "BalanceReason",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "AmountLayout",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "DebitsCarryMinus",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "IndicatorColumn",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "LayoutConfirmed",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "MoneyInMarker",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "MoneyOutMarker",
                table: "CsvMappingProfiles");
        }
    }
}
