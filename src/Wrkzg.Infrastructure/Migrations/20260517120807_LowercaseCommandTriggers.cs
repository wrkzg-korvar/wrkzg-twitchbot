using Microsoft.EntityFrameworkCore.Migrations;

namespace Wrkzg.Infrastructure.Migrations;

/// <summary>
/// Data migration: Normalizes all existing command triggers and aliases to lowercase.
/// Fixes commands imported before the ToLowerInvariant() fix was applied to the import pipeline.
/// </summary>
public partial class LowercaseCommandTriggers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Lowercase all command triggers
        migrationBuilder.Sql("UPDATE Commands SET Trigger = LOWER(Trigger);");

        // Lowercase all command aliases (stored as JSON array text, e.g. '["!DC","!Disc"]')
        // LOWER() on the full JSON string is safe because JSON structural characters
        // ([, ], ", ,) are not alphabetic and are unaffected by LOWER().
        migrationBuilder.Sql("UPDATE Commands SET Aliases = LOWER(Aliases) WHERE Aliases IS NOT NULL AND Aliases != '[]';");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data migration — cannot be reversed (original casing is lost)
    }
}
