using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameGameCommandOverrideToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The '!game' system command was renamed to '!category' (primary trigger) with the
            // new alias '!ctgy'. SystemCommandOverrides.Trigger is the primary key, so without
            // this data migration every existing user override (enable/disable state and custom
            // response template) would be orphaned and the command would silently fall back to
            // its defaults. Move the row to the new key to preserve the user's configuration.
            //
            // The NOT EXISTS guard keeps the UPDATE from violating the primary key. Override rows
            // are only ever written for primary triggers (CommandEndpoints validates against
            // ISystemCommand.Trigger), so a '!category' row cannot originate from the former
            // alias — the guard is purely defensive and makes this migration idempotent.
            migrationBuilder.Sql(
                """
                UPDATE SystemCommandOverrides
                SET Trigger = '!category'
                WHERE Trigger = '!game'
                  AND NOT EXISTS (SELECT 1 FROM SystemCommandOverrides WHERE Trigger = '!category');
                """);

            // Any '!game' row still present here duplicates an existing '!category' row.
            // '!category' is the trigger the current code reads, so it wins.
            migrationBuilder.Sql("DELETE FROM SystemCommandOverrides WHERE Trigger = '!game';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the key rename so a downgraded build finds its '!game' override again.
            migrationBuilder.Sql(
                """
                UPDATE SystemCommandOverrides
                SET Trigger = '!game'
                WHERE Trigger = '!category'
                  AND NOT EXISTS (SELECT 1 FROM SystemCommandOverrides WHERE Trigger = '!game');
                """);
        }
    }
}
