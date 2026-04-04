using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A saved chat quote. Stores the text, who said it, who saved it, and the game being played.
/// </summary>
public class Quote
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Sequential quote number (1, 2, 3...). Displayed to users.</summary>
    public int Number { get; set; }

    /// <summary>The quote text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Who said it (display name).</summary>
    public string QuotedUser { get; set; } = string.Empty;

    /// <summary>Who saved the quote (display name).</summary>
    public string SavedBy { get; set; } = string.Empty;

    /// <summary>The game/category being played when the quote was saved. Null if unknown/offline.</summary>
    public string? GameName { get; set; }

    /// <summary>When the quote was saved.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
