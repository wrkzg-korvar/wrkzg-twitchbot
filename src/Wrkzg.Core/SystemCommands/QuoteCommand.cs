using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Manages chat quotes. Sub-commands:
///   !quote           → random quote
///   !quote 5         → quote #5
///   !quote add text  → save a new quote (Mod/Broadcaster)
///   !quote delete 5  → delete quote #5 (Mod/Broadcaster)
/// </summary>
public class QuoteCommand : ISystemCommand
{
    public string Trigger => "!quote";
    public string[] Aliases => new[] { "!q", "!addquote", "!quoteadd" };
    public string Description => "View, add, or delete quotes. Usage: !quote [number|add <text>|delete <number>]";
    public string? DefaultResponseTemplate => "Quote #{number}: \"{text}\" — {quoteduser} [{game}]";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;

    public QuoteCommand(IServiceScopeFactory scopeFactory, ITwitchChatClient chatClient)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        string content = message.Content.Trim();
        string args;

        // Handle !addquote and !quoteadd aliases → treat as "!quote add ..."
        if (content.StartsWith("!addquote", StringComparison.OrdinalIgnoreCase))
        {
            string addText = content.Length > "!addquote".Length
                ? content["!addquote".Length..].Trim()
                : string.Empty;
            return await HandleAddAsync(message, addText, ct);
        }

        if (content.StartsWith("!quoteadd", StringComparison.OrdinalIgnoreCase))
        {
            string addText = content.Length > "!quoteadd".Length
                ? content["!quoteadd".Length..].Trim()
                : string.Empty;
            return await HandleAddAsync(message, addText, ct);
        }

        // Extract args after !quote or !q
        if (content.StartsWith("!quote", StringComparison.OrdinalIgnoreCase))
        {
            args = content.Length > "!quote".Length
                ? content["!quote".Length..].Trim()
                : string.Empty;
        }
        else if (content.StartsWith("!q", StringComparison.OrdinalIgnoreCase))
        {
            args = content.Length > "!q".Length
                ? content["!q".Length..].Trim()
                : string.Empty;
        }
        else
        {
            args = string.Empty;
        }

        // Sub-command: !quote add <text>
        if (args.StartsWith("add ", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleAddAsync(message, args[4..].Trim(), ct);
        }

        if (string.Equals(args, "add", StringComparison.OrdinalIgnoreCase))
        {
            return $"@{message.DisplayName}, usage: !quote add <text>";
        }

        // Sub-command: !quote delete <number> or !quote del <number>
        if (args.StartsWith("delete ", StringComparison.OrdinalIgnoreCase)
            || args.StartsWith("del ", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleDeleteAsync(message, args, ct);
        }

        // !quote <number> or !quote (random)
        if (int.TryParse(args, out int number))
        {
            return await HandleGetByNumberAsync(number, ct);
        }

        return await HandleRandomAsync(ct);
    }

    private async Task<string?> HandleAddAsync(ChatMessage message, string quoteText, CancellationToken ct)
    {
        // Permission: Mod + Broadcaster only
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods and the broadcaster can add quotes.";
        }

        if (string.IsNullOrWhiteSpace(quoteText))
        {
            return $"@{message.DisplayName}, usage: !quote add <text>";
        }

        if (quoteText.Length > 500)
        {
            return $"@{message.DisplayName}, quote text must be 500 characters or less.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IQuoteRepository repo = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        ITwitchHelixClient helix = scope.ServiceProvider.GetRequiredService<ITwitchHelixClient>();

        // Try to get the current game name
        string? gameName = null;
        string? channel = _chatClient.JoinedChannel;
        if (channel is not null)
        {
            StreamInfo? stream = await helix.GetStreamAsync(channel, ct);
            if (stream is not null && !string.IsNullOrEmpty(stream.GameName))
            {
                gameName = stream.GameName;
            }
        }

        int nextNumber = await repo.GetNextNumberAsync(ct);
        Quote quote = new()
        {
            Number = nextNumber,
            Text = quoteText,
            QuotedUser = message.DisplayName,
            SavedBy = message.DisplayName,
            GameName = gameName
        };

        await repo.CreateAsync(quote, ct);
        return $"Quote #{nextNumber} added: \"{quoteText}\"";
    }

    private async Task<string?> HandleDeleteAsync(ChatMessage message, string args, CancellationToken ct)
    {
        // Permission: Mod + Broadcaster only
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods and the broadcaster can delete quotes.";
        }

        // Parse the number from "delete 5" or "del 5"
        string numPart = args.Contains(' ') ? args[(args.IndexOf(' ') + 1)..].Trim() : string.Empty;
        if (!int.TryParse(numPart, out int number))
        {
            return $"@{message.DisplayName}, usage: !quote delete <number>";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IQuoteRepository repo = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();

        Quote? quote = await repo.GetByNumberAsync(number, ct);
        if (quote is null)
        {
            return $"@{message.DisplayName}, quote #{number} doesn't exist.";
        }

        await repo.DeleteAsync(quote.Id, ct);
        return $"Quote #{number} deleted.";
    }

    private async Task<string?> HandleGetByNumberAsync(int number, CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IQuoteRepository repo = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();

        Quote? quote = await repo.GetByNumberAsync(number, ct);
        if (quote is null)
        {
            return $"Quote #{number} doesn't exist.";
        }

        return FormatQuote(quote);
    }

    private async Task<string?> HandleRandomAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IQuoteRepository repo = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();

        Quote? quote = await repo.GetRandomAsync(ct);
        if (quote is null)
        {
            return "No quotes saved yet. Use !quote add <text> to add one.";
        }

        return FormatQuote(quote);
    }

    private static string FormatQuote(Quote quote)
    {
        string game = !string.IsNullOrEmpty(quote.GameName) ? quote.GameName : "unknown";
        return $"Quote #{quote.Number}: \"{quote.Text}\" — {quote.QuotedUser} [{game}]";
    }
}
