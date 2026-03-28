import React from "react";

/**
 * Renders message content with Twitch emotes replaced by images.
 * Uses emote positions from the Twitch IRC tags (emoteId → ["startIndex-endIndex"]).
 * Works for ALL Twitch emotes (global, channel, subscriber, bits).
 */
export function renderWithEmotes(
  content: string,
  emotes: Record<string, string[]> | null | undefined,
  emoteSize: number = 24,
): React.ReactNode {
  if (!emotes || Object.keys(emotes).length === 0) {
    return content;
  }

  // Build sorted list of emote replacements
  const replacements: { start: number; end: number; emoteId: string }[] = [];

  for (const [emoteId, positions] of Object.entries(emotes)) {
    for (const pos of positions) {
      const [startStr, endStr] = pos.split("-");
      const start = parseInt(startStr, 10);
      const end = parseInt(endStr, 10);
      if (!isNaN(start) && !isNaN(end)) {
        replacements.push({ start, end, emoteId });
      }
    }
  }

  replacements.sort((a, b) => a.start - b.start);

  if (replacements.length === 0) {
    return content;
  }

  const parts: React.ReactNode[] = [];
  let lastIndex = 0;

  for (let i = 0; i < replacements.length; i++) {
    const { start, end, emoteId } = replacements[i];

    // Text before this emote
    if (start > lastIndex) {
      parts.push(<span key={`t${i}`}>{content.slice(lastIndex, start)}</span>);
    }

    // Emote image
    const emoteName = content.slice(start, end + 1);
    parts.push(
      <img
        key={`e${i}`}
        src={`https://static-cdn.jtvnw.net/emoticons/v2/${emoteId}/default/dark/2.0`}
        alt={emoteName}
        title={emoteName}
        style={{
          display: "inline-block",
          verticalAlign: "middle",
          height: `${emoteSize}px`,
          width: "auto",
          margin: "0 1px",
        }}
      />,
    );

    lastIndex = end + 1;
  }

  // Remaining text after last emote
  if (lastIndex < content.length) {
    parts.push(<span key="tail">{content.slice(lastIndex)}</span>);
  }

  return <>{parts}</>;
}
