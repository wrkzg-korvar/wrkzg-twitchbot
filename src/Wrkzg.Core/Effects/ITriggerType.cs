using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Effects;

/// <summary>
/// Defines a trigger type for the Effect System.
/// Triggers determine WHEN an effect list is activated.
/// Registered via DI — plug-and-play.
/// </summary>
public interface ITriggerType
{
    /// <summary>Unique identifier (e.g. "command", "event.follow", "hotkey").</summary>
    string Id { get; }

    /// <summary>Human-readable name for the dashboard (e.g. "Chat Command").</summary>
    string DisplayName { get; }

    /// <summary>Parameter keys this trigger expects in TriggerConfig JSON.</summary>
    string[] ParameterKeys { get; }

    /// <summary>Checks if this trigger matches the given context.</summary>
    Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default);
}
