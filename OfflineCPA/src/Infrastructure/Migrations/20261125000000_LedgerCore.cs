using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <summary>
/// Hand-written (no live PostgreSQL when authored). Replaces the line-level
/// "JournalEntries" table with Periods / JournalEntries (header) / Postings /
/// LedgerAuditRecords and adds database triggers that guard posted history.
/// </summary>
[DbContext(typeof(OfflineCPADbContext))]
[Migration("20261125000000_LedgerCore")]
public partial class LedgerCore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JournalEntries");

        migrationBuilder.CreateTable(
            name: "LedgerAuditRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LocalUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Action = table.Column<int>(type: "integer", nullable: false),
                JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                PeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerAuditRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Periods",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                IsLocked = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Periods", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "JournalEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                PeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReversesEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                ReversedByEntryId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JournalEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_JournalEntries_Periods_PeriodId",
                    column: x => x.PeriodId,
                    principalTable: "Periods",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Postings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Postings", x => x.Id);
                table.ForeignKey(
                    name: "FK_Postings_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Postings_JournalEntries_JournalEntryId",
                    column: x => x.JournalEntryId,
                    principalTable: "JournalEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_JournalEntries_PeriodId",
            table: "JournalEntries",
            column: "PeriodId");

        migrationBuilder.CreateIndex(
            name: "IX_JournalEntries_Status",
            table: "JournalEntries",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Postings_AccountId",
            table: "Postings",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_Postings_JournalEntryId",
            table: "Postings",
            column: "JournalEntryId");

        // Database guard. Status: 0 = Draft, 1 = Posted, 2 = Reversed.

        // Postings of a non-draft entry cannot be changed or deleted.
        // If the parent row is gone (cascade from a deleted draft), the change is allowed.
        migrationBuilder.Sql("""
            CREATE FUNCTION "Fn_Postings_Immutable"() RETURNS trigger AS $$
            DECLARE
                old_status integer;
                new_status integer;
            BEGIN
                SELECT "Status" INTO old_status FROM "JournalEntries" WHERE "Id" = OLD."JournalEntryId";
                IF FOUND AND old_status <> 0 THEN
                    RAISE EXCEPTION 'Postings of a posted or reversed journal entry cannot be changed or deleted.';
                END IF;
                IF TG_OP = 'UPDATE' THEN
                    SELECT "Status" INTO new_status FROM "JournalEntries" WHERE "Id" = NEW."JournalEntryId";
                    IF FOUND AND new_status <> 0 THEN
                        RAISE EXCEPTION 'Postings cannot be moved into a posted or reversed journal entry.';
                    END IF;
                    RETURN NEW;
                END IF;
                RETURN OLD;
            END;
            $$ LANGUAGE plpgsql;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER "TR_Postings_Immutable"
            BEFORE UPDATE OR DELETE ON "Postings"
            FOR EACH ROW EXECUTE FUNCTION "Fn_Postings_Immutable"();
            """);

        // Journal entries are never deleted. Once an entry leaves Draft, its
        // Date, Memo and PeriodId are frozen. Status, PostedAtUtc and
        // ReversedByEntryId stay writable so Post (0->1) and Reverse (1->2) work.
        migrationBuilder.Sql("""
            CREATE FUNCTION "Fn_JournalEntries_Immutable"() RETURNS trigger AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    RAISE EXCEPTION 'Journal entries cannot be deleted.';
                END IF;
                IF OLD."Status" <> 0 AND (
                    NEW."Date" IS DISTINCT FROM OLD."Date"
                    OR NEW."Memo" IS DISTINCT FROM OLD."Memo"
                    OR NEW."PeriodId" IS DISTINCT FROM OLD."PeriodId") THEN
                    RAISE EXCEPTION 'Date, Memo and PeriodId of a posted or reversed journal entry cannot be changed.';
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER "TR_JournalEntries_Immutable"
            BEFORE UPDATE OR DELETE ON "JournalEntries"
            FOR EACH ROW EXECUTE FUNCTION "Fn_JournalEntries_Immutable"();
            """);

        // Audit records are append-only.
        migrationBuilder.Sql("""
            CREATE FUNCTION "Fn_LedgerAuditRecords_AppendOnly"() RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'Ledger audit records are append-only.';
            END;
            $$ LANGUAGE plpgsql;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER "TR_LedgerAuditRecords_AppendOnly"
            BEFORE UPDATE OR DELETE ON "LedgerAuditRecords"
            FOR EACH ROW EXECUTE FUNCTION "Fn_LedgerAuditRecords_AppendOnly"();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TRIGGER "TR_LedgerAuditRecords_AppendOnly" ON "LedgerAuditRecords";""");
        migrationBuilder.Sql("""DROP FUNCTION "Fn_LedgerAuditRecords_AppendOnly"();""");
        migrationBuilder.Sql("""DROP TRIGGER "TR_JournalEntries_Immutable" ON "JournalEntries";""");
        migrationBuilder.Sql("""DROP FUNCTION "Fn_JournalEntries_Immutable"();""");
        migrationBuilder.Sql("""DROP TRIGGER "TR_Postings_Immutable" ON "Postings";""");
        migrationBuilder.Sql("""DROP FUNCTION "Fn_Postings_Immutable"();""");

        migrationBuilder.DropTable(
            name: "Postings");

        migrationBuilder.DropTable(
            name: "LedgerAuditRecords");

        migrationBuilder.DropTable(
            name: "JournalEntries");

        migrationBuilder.DropTable(
            name: "Periods");

        migrationBuilder.CreateTable(
            name: "JournalEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JournalEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_JournalEntries_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_JournalEntries_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_JournalEntries_AccountId",
            table: "JournalEntries",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_JournalEntries_TransactionId",
            table: "JournalEntries",
            column: "TransactionId");
    }
}
