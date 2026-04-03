using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Core.ChatGames;

/// <summary>
/// Loads and resolves configurable message templates for chat games.
/// Each message has a Settings key (Games.{Game}.Msg.{Key}) and a default.
/// Templates support {variable} placeholders resolved at runtime.
/// </summary>
public class GameMessageTemplates
{
    private readonly Dictionary<string, string> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaults;
    private readonly string _gameName;

    public GameMessageTemplates(string gameName, Dictionary<string, string> defaults)
    {
        _gameName = gameName;
        _defaults = defaults;

        // Initialize with defaults
        foreach (KeyValuePair<string, string> kvp in defaults)
        {
            _templates[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Loads custom templates from settings, falling back to defaults.
    /// </summary>
    public async Task LoadAsync(IServiceScopeFactory scopeFactory, CancellationToken ct = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        foreach (string key in _defaults.Keys)
        {
            string settingsKey = $"Games.{_gameName}.Msg.{key}";
            string? custom = await settings.GetAsync(settingsKey, ct);
            if (!string.IsNullOrWhiteSpace(custom))
            {
                _templates[key] = custom;
            }
            else
            {
                _templates[key] = _defaults[key];
            }
        }
    }

    /// <summary>
    /// Gets a message template by key, resolving variables.
    /// </summary>
    public string Get(string key, params (string name, string value)[] variables)
    {
        string template = _templates.GetValueOrDefault(key) ?? _defaults.GetValueOrDefault(key) ?? key;

        foreach ((string name, string value) in variables)
        {
            template = template.Replace($"{{{name}}}", value);
        }

        return template;
    }

    /// <summary>
    /// Returns all template keys with their current values (for the API/dashboard).
    /// </summary>
    public Dictionary<string, string> GetAll() => new(_templates);

    /// <summary>
    /// Returns all template keys with their default values (for reset).
    /// </summary>
    public Dictionary<string, string> GetDefaults() => new(_defaults);
}
