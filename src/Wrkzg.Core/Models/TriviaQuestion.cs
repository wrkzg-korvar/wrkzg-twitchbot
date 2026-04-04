using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// A trivia question for the trivia chat game.
/// </summary>
public class TriviaQuestion
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The question text.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>The canonical correct answer.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>Case-insensitive alternative accepted answers.</summary>
    public List<string> AcceptedAnswers { get; set; } = new();

    /// <summary>Optional category (e.g. "Gaming", "Twitch", "General").</summary>
    public string? Category { get; set; }

    /// <summary>True if user-created, false if built-in.</summary>
    public bool IsCustom { get; set; }

    /// <summary>When this question was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
