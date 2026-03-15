import { useAuthStatus } from "../../hooks/useAuthStatus";
import { startOAuthFlow } from "../../api/auth";

interface BotConnectStepProps {
  onNext: () => void;
  onBack: () => void;
}

export function BotConnectStep({ onNext, onBack }: BotConnectStepProps) {
  const { bot } = useAuthStatus();

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-white">Connect Bot Account</h2>
        <p className="mt-2 text-sm text-gray-400">
          This is the Twitch account your bot will use to send and read chat
          messages. It can be your main account, but we recommend creating a
          separate account with a custom bot name (e.g. "MeinStreamBot").
        </p>
      </div>

      <div className="space-y-3 rounded-lg border border-gray-800 bg-gray-900/50 p-5">
        <h3 className="text-sm font-semibold text-gray-200">How it works</h3>
        <ul className="space-y-2 text-sm text-gray-400">
          <li className="flex items-start gap-2">
            <span className="mt-1 text-purple-400">•</span>
            <span>
              Clicking "Connect" will open a Twitch login popup. Log in with
              the account you want the bot to use.
            </span>
          </li>
          <li className="flex items-start gap-2">
            <span className="mt-1 text-purple-400">•</span>
            <span>
              Twitch will ask you to authorize <strong className="text-gray-300">chat:read</strong>{" "}
              and <strong className="text-gray-300">chat:edit</strong> permissions.
            </span>
          </li>
          <li className="flex items-start gap-2">
            <span className="mt-1 text-purple-400">•</span>
            <span>
              After authorizing, the popup closes automatically and you can continue.
            </span>
          </li>
        </ul>
      </div>

      {/* ─── Connection Status ──────────────────────────────── */}
      <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-5">
        {bot.isAuthenticated ? (
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-green-500/20 text-lg">
              ✓
            </span>
            <div>
              <p className="text-sm font-semibold text-green-400">Bot account connected</p>
              <p className="text-xs text-gray-400">
                Logged in as{" "}
                <span className="text-purple-400 font-medium">{bot.twitchUsername}</span>
              </p>
            </div>
          </div>
        ) : (
          <div className="text-center py-2">
            <p className="mb-4 text-sm text-gray-400">No bot account connected yet.</p>
            <button
              onClick={() => startOAuthFlow("bot")}
              className="rounded-lg bg-purple-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-purple-700 transition-colors"
            >
              Connect Bot Account with Twitch
            </button>
          </div>
        )}
      </div>

      {/* ─── Navigation ─────────────────────────────────────── */}
      <div className="flex items-center justify-between pt-2">
        <button
          onClick={onBack}
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-gray-400 hover:text-gray-200 transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={onNext}
          disabled={!bot.isAuthenticated}
          className="rounded-lg bg-purple-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Continue →
        </button>
      </div>
    </div>
  );
}
