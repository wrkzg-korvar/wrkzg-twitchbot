namespace Wrkzg.Core.Models;

/// <summary>
/// User-Overrides für System Commands.
/// Speichert angepassten Response-Text und Enabled-Status.
/// Der Trigger ist immutable und dient als Primary Key.
/// </summary>
public class SystemCommandOverride
{
    /// <summary>
    /// Der Command-Trigger (z.B. "!points"). Nicht änderbar.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// Benutzerdefinierter Response-Text. Null = Standard-Response der C#-Klasse verwenden.
    /// </summary>
    public string? CustomResponseTemplate { get; set; }

    /// <summary>
    /// Ob der Command aktiv ist. Default: true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
