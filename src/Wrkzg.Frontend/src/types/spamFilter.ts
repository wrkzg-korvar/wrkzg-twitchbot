export interface SpamFilterConfig {
  linksEnabled: boolean;
  linksTimeoutSeconds: number;
  linksSubsExempt: boolean;
  linksModsExempt: boolean;
  linkWhitelist: string;
  capsEnabled: boolean;
  capsMinLength: number;
  capsMaxPercent: number;
  capsTimeoutSeconds: number;
  capsSubsExempt: boolean;
  bannedWordsEnabled: boolean;
  bannedWordsList: string;
  bannedWordsTimeoutSeconds: number;
  bannedWordsSubsExempt: boolean;
  emoteSpamEnabled: boolean;
  emoteSpamMaxEmotes: number;
  emoteSpamTimeoutSeconds: number;
  emoteSpamSubsExempt: boolean;
  repeatEnabled: boolean;
  repeatMaxCount: number;
  repeatTimeoutSeconds: number;
  repeatSubsExempt: boolean;
}
