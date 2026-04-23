using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Birds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteSyncCursorEntityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(
                    """
                    CREATE TABLE IF NOT EXISTS "RemoteSyncCursors" (
                        "CursorKey" TEXT NOT NULL CONSTRAINT "PK_RemoteSyncCursors" PRIMARY KEY,
                        "LastSyncedAtUtc" TEXT NULL,
                        "LastSyncedEntityId" TEXT NULL
                    );
                    """);
            }
            else
            {
                migrationBuilder.Sql(
                    """
                    CREATE TABLE IF NOT EXISTS "RemoteSyncCursors" (
                        "CursorKey" text NOT NULL CONSTRAINT "PK_RemoteSyncCursors" PRIMARY KEY,
                        "LastSyncedAtUtc" timestamp without time zone NULL,
                        "LastSyncedEntityId" uuid NULL
                    );
                    """);

                migrationBuilder.Sql(
                    """
                    ALTER TABLE "RemoteSyncCursors"
                    ADD COLUMN IF NOT EXISTS "LastSyncedEntityId" uuid NULL;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""DROP TABLE IF EXISTS "RemoteSyncCursors";""");
            }
            else
            {
                migrationBuilder.Sql(
                    """
                    ALTER TABLE "RemoteSyncCursors"
                    DROP COLUMN IF EXISTS "LastSyncedEntityId";
                    """);
            }
        }
    }
}
