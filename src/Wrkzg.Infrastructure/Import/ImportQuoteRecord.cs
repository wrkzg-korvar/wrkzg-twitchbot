using System;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parsed quote from a DeepBot chanmsgconfig save file.
/// </summary>
public class ImportQuoteRecord
{
    /// <summary>Quote number from DeepBot.</summary>
    public int Number { get; set; }

    /// <summary>Who said it.</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>The quote text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Who added the quote.</summary>
    public string AddedBy { get; set; } = string.Empty;

    /// <summary>When the quote was added.</summary>
    public DateTimeOffset? AddedOn { get; set; }
}
