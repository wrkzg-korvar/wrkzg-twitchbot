namespace Wrkzg.Core.Models;

/// <summary>
/// Key-value setting stored in the database.
/// Used for runtime-configurable options (points per minute, sub multiplier, etc.).
/// </summary>
public class Setting
{
    /// <summary>Dot-separated key, e.g. "Points.PerMinute".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>String value — parsed to the appropriate type by the consuming service.</summary>
    public string Value { get; set; } = string.Empty;
}
