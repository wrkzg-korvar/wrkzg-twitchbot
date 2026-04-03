using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Effects;

/// <summary>
/// Defines an effect type for the Effect System.
/// Effects are actions that execute in sequence when triggered.
/// </summary>
public interface IEffectType
{
    /// <summary>Unique identifier (e.g. "chat_message", "wait", "counter").</summary>
    string Id { get; }

    /// <summary>Human-readable name for the dashboard.</summary>
    string DisplayName { get; }

    /// <summary>Parameter keys this effect expects.</summary>
    string[] ParameterKeys { get; }

    /// <summary>Executes the effect action.</summary>
    Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default);
}
