import { useAuthStatus } from "../../hooks/useAuthStatus";
import { startOAuthFlow } from "../../api/auth";

interface BroadcasterConnectStepProps {
  onNext: () => void;
  onBack: () => void;
}

export function BroadcasterConnectStep({ onNext, onBack }: BroadcasterConnectStepProps) {
  const { broadcaster } = useAuthStatus();

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-white">Connect Broadcaster Account</h2>
        <p className="mt-2 text-sm text-gray-400">
          This is your main Twitch channel account. Wrkzg needs it to access
          follower data, subscriber info, manage polls, and receive real-time
          events like new follows and subs.
        </p>
      </div>

      <div className="rounded-lg border border-amber-800/30 bg-amber-950/20 p-4">
        <p className="text-sm text-amber-400/80">
          <strong>Important:</strong> Log in with your{" "}
          <strong>streamer/broadcaster account</strong> this time — not the bot
          account from the previous step.
        </p>
      </div>

      <div className="space-y-3 rounded-lg border border-gray-800 bg-gray-900/50 p-5">
        <h3 className="text-sm font-semibold text-gray-200">Permissions requested</h3>
        <ul className="space-y-1.5 text-xs text-gray-400">
          <li><code className="text-gray-300">moderator:read:followers</code> — Read follower list</li>
          <li><code className="text-gray-300">channel:read:subscriptions</code> — Read subscriber info</li>
          <li><code className="text-gray-300">channel:read:polls</code> — Read polls</li>
          <li><code className="text-gray-300">channel:manage:polls</code> — Create and end polls</li>
          <li><code className="text-gray-300">bits:read</code> — Read bits/cheering events</li>
        </ul>
      </div>

      {/* ─── Connection Status ──────────────────────────────── */}
      <div className="rounded-lg border border-gray-800 bg-gray-900/50 p-5">
        {broadcaster.isAuthenticated ? (
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-green-500/20 text-lg">
              ✓
            </span>
            <div>
              <p className="text-sm font-semibold text-green-400">Broadcaster account connected</p>
              <p className="text-xs text-gray-400">
                Logged in as{" "}
                <span className="text-purple-400 font-medium">{broadcaster.twitchUsername}</span>
              </p>
            </div>
          </div>
        ) : (
          <div className="text-center py-2">
            <p className="mb-4 text-sm text-gray-400">No broadcaster account connected yet.</p>
            <button
              onClick={() => startOAuthFlow("broadcaster")}
              className="rounded-lg bg-purple-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-purple-700 transition-colors"
            >
              Connect Broadcaster with Twitch
            </button>
          </div>
        )}
      </div>

      <div className="flex items-center justify-between pt-2">
        <button
          onClick={onBack}
          className="rounded-lg px-4 py-2.5 text-sm font-medium text-gray-400 hover:text-gray-200 transition-colors"
        >
          ← Back
        </button>
        <button
          onClick={onNext}
          disabled={!broadcaster.isAuthenticated}
          className="rounded-lg bg-purple-600 px-6 py-2.5 text-sm font-semibold text-white hover:bg-purple-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Continue →
        </button>
      </div>
    </div>
  );
}
