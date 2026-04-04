using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A user-created custom overlay with HTML, CSS, and JavaScript.
/// </summary>
public class CustomOverlay
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description.</summary>
    public string? Description { get; set; }

    /// <summary>HTML content.</summary>
    public string Html { get; set; } = "";

    /// <summary>CSS content.</summary>
    public string Css { get; set; } = "";

    /// <summary>JavaScript content.</summary>
    public string JavaScript { get; set; } = "";

    /// <summary>JSON schema for configurable fields.</summary>
    public string FieldDefinitions { get; set; } = "{}";

    /// <summary>Current field values (JSON).</summary>
    public string FieldValues { get; set; } = "{}";

    /// <summary>Recommended width for OBS.</summary>
    public int Width { get; set; } = 800;

    /// <summary>Recommended height for OBS.</summary>
    public int Height { get; set; } = 600;

    /// <summary>Whether this overlay is active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>When this overlay was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When this overlay was last modified.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
