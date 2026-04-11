using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Helpers;

public static class TwitchMessageHelper
{
    public const int MaxMessageLength = 500;

    /// <summary>
    /// Truncates a message to fit within Twitch's character limit.
    /// Appends "..." if truncated.
    /// </summary>
    public static string Truncate(string message)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= MaxMessageLength)
        {
            return message;
        }

        return message[..(MaxMessageLength - 3)] + "...";
    }

    /// <summary>
    /// Splits a message into multiple parts that each fit within Twitch's limit.
    /// Splits at word boundaries where possible.
    /// Adds page indicators like "(1/3)" when splitting.
    /// </summary>
    public static List<string> SplitMessage(string message, string? pagePrefix = null)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= MaxMessageLength)
        {
            return new List<string> { message };
        }

        List<string> parts = new();
        int reservedForPageIndicator = 10; // e.g. " (1/3)"
        int chunkSize = MaxMessageLength - reservedForPageIndicator;

        if (!string.IsNullOrEmpty(pagePrefix))
        {
            chunkSize -= pagePrefix.Length;
        }

        int start = 0;
        while (start < message.Length)
        {
            int end = Math.Min(start + chunkSize, message.Length);

            // Try to split at a word boundary
            if (end < message.Length)
            {
                int lastSpace = message.LastIndexOf(' ', end, end - start);
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            parts.Add(message[start..end].Trim());
            start = end;
        }

        // Add page indicators
        if (parts.Count > 1)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                string prefix = !string.IsNullOrEmpty(pagePrefix) ? pagePrefix + " " : "";
                parts[i] = $"{prefix}{parts[i]} ({i + 1}/{parts.Count})";
            }
        }

        return parts;
    }
}
