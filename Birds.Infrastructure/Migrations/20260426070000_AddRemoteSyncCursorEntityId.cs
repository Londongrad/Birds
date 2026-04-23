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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "RemoteSyncCursors"
                DROP COLUMN IF EXISTS "LastSyncedEntityId";
                """);
        }
    }
}
