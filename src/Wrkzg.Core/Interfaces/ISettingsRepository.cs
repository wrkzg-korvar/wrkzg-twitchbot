using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for key-value runtime settings.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Retrieves a single setting value by its key.
    /// </summary>
    /// <param name="key">The setting key to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The setting value, or null if the key does not exist.</returns>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all stored settings as a key-value dictionary.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary of all setting key-value pairs.</returns>
    Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a single setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync(string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates multiple settings in a single operation.
    /// </summary>
    /// <param name="settings">A dictionary of setting key-value pairs to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetManyAsync(IDictionary<string, string> settings, CancellationToken ct = default);

    /// <summary>
    /// Deletes a setting by its key.
    /// </summary>
    /// <param name="key">The setting key to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the setting existed and was deleted; false if the key was not found.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}
