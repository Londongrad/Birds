using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Birds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBirdSyncStampUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "SyncStampUtc",
                    table: "Birds",
                    type: "TEXT",
                    nullable: true);

                migrationBuilder.Sql(
                    """
                    UPDATE "Birds"
                    SET "SyncStampUtc" = COALESCE("UpdatedAt", "CreatedAt", strftime('%Y-%m-%d %H:%M:%f', 'now'))
                    WHERE "SyncStampUtc" IS NULL;
                    """);

                migrationBuilder.AlterColumn<DateTime>(
                    name: "SyncStampUtc",
                    table: "Birds",
                    type: "TEXT",
                    nullable: false,
                    oldClrType: typeof(DateTime),
                    oldType: "TEXT",
                    oldNullable: true);
            }
            else
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "SyncStampUtc",
                    table: "Birds",
                    type: "timestamp without time zone",
                    nullable: true);

                migrationBuilder.Sql(
                    """
                    UPDATE "Birds"
                    SET "SyncStampUtc" = COALESCE("UpdatedAt", "CreatedAt", CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
                    WHERE "SyncStampUtc" IS NULL;
                    """);

                migrationBuilder.AlterColumn<DateTime>(
                    name: "SyncStampUtc",
                    table: "Birds",
                    type: "timestamp without time zone",
                    nullable: false,
                    oldClrType: typeof(DateTime),
                    oldType: "timestamp without time zone",
                    oldNullable: true);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Birds_SyncStampUtc",
                table: "Birds",
                column: "SyncStampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Birds_SyncStampUtc",
                table: "Birds");

            migrationBuilder.DropColumn(
                name: "SyncStampUtc",
                table: "Birds");
        }
    }
}
