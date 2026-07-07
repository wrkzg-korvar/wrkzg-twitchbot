using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wrkzg.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetFollowDatesForRefetch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FollowDate was previously stored as the bot-observation time (UtcNow) instead of the
            // real Twitch follow date. Reset to NULL so the corrected code re-fetches the true value.
            migrationBuilder.Sql("UPDATE Users SET FollowDate = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: the original (incorrect) values are intentionally discarded.
        }
    }
}
