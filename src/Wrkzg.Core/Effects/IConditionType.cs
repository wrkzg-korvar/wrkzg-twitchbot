using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Effects;

/// <summary>
/// Defines a condition type for the Effect System.
/// Conditions are optional gates that must pass before effects execute.
/// </summary>
public interface IConditionType
{
    /// <summary>Unique identifier (e.g. "role_check", "random_chance").</summary>
    string Id { get; }

    /// <summary>Human-readable name for the dashboard.</summary>
    string DisplayName { get; }

    /// <summary>Parameter keys this condition expects.</summary>
    string[] ParameterKeys { get; }

    /// <summary>Evaluates whether the condition passes.</summary>
    Task<bool> EvaluateAsync(EffectConditionContext context, CancellationToken ct = default);
}
