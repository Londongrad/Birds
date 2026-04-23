using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Birds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalSyncSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Birds",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAlive",
                table: "Birds",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Birds",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Departure",
                table: "Birds",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Arrival",
                table: "Birds",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Birds",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "RemoteSyncCursors" (
                    "CursorKey" TEXT NOT NULL CONSTRAINT "PK_RemoteSyncCursors" PRIMARY KEY,
                    "LastSyncedAtUtc" TEXT NULL,
                    "LastSyncedEntityId" TEXT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "SyncOperations" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_SyncOperations" PRIMARY KEY,
                    "AggregateType" TEXT NOT NULL,
                    "AggregateId" TEXT NOT NULL,
                    "OperationType" TEXT NOT NULL,
                    "PayloadJson" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NOT NULL,
                    "RetryCount" INTEGER NOT NULL DEFAULT 0,
                    "LastAttemptAtUtc" TEXT NULL,
                    "LastError" TEXT NULL
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SyncOperations_AggregateType_AggregateId"
                ON "SyncOperations" ("AggregateType", "AggregateId");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_SyncOperations_CreatedAtUtc"
                ON "SyncOperations" ("CreatedAtUtc");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteSyncCursors");

            migrationBuilder.DropTable(
                name: "SyncOperations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Birds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAlive",
                table: "Birds",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Birds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Departure",
                table: "Birds",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Arrival",
                table: "Birds",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Birds",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }
    }
}
