namespace Wrkzg.Core.Models;

/// <summary>
/// User overrides for system commands.
/// Stores a customized response template and enabled status.
/// The trigger is immutable and serves as the primary key.
/// </summary>
public class SystemCommandOverride
{
    /// <summary>
    /// The command trigger (e.g. "!points"). Not modifiable.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// User-defined response template. Null = use the default response of the C# class.
    /// </summary>
    public string? CustomResponseTemplate { get; set; }

    /// <summary>
    /// Whether the command is active. Default: true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
