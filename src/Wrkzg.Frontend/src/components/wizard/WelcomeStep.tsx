interface WelcomeStepProps {
  onNext: () => void;
}

export function WelcomeStep({ onNext }: WelcomeStepProps) {
  return (
    <div className="space-y-8">
      <div className="text-center">
        <h1 className="text-3xl font-bold text-white">
          Welcome to <span className="text-purple-400">Wrkzg</span>
        </h1>
        <p className="mt-3 text-gray-400">
          Your self-hosted Twitch community bot with a built-in dashboard.
        </p>
      </div>

      <div className="space-y-4 rounded-lg border border-gray-800 bg-gray-900/50 p-6">
        <p className="text-sm leading-relaxed text-gray-300">
          Let's get you set up! This wizard will guide you through connecting
          Wrkzg to your Twitch channel. It takes about 5 minutes and involves
          these steps:
        </p>

        <ol className="space-y-3 text-sm text-gray-400">
          <li className="flex items-start gap-3">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-purple-500/20 text-xs font-bold text-purple-400">
              1
            </span>
            <span>
              <strong className="text-gray-200">Create a Twitch App</strong> — Register a free
              developer application on Twitch (takes 2 minutes).
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-purple-500/20 text-xs font-bold text-purple-400">
              2
            </span>
            <span>
              <strong className="text-gray-200">Connect Bot Account</strong> — The Twitch account
              your bot will use to chat (can be your main account or a separate one).
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-purple-500/20 text-xs font-bold text-purple-400">
              3
            </span>
            <span>
              <strong className="text-gray-200">Connect Broadcaster Account</strong> — Your
              main channel account for API access (followers, subs, polls).
            </span>
          </li>
          <li className="flex items-start gap-3">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-purple-500/20 text-xs font-bold text-purple-400">
              4
            </span>
            <span>
              <strong className="text-gray-200">Set your channel</strong> — Tell the bot which
              chat to join, and you're ready to go!
            </span>
          </li>
        </ol>
      </div>

      <div className="space-y-3 rounded-lg border border-gray-800 bg-gray-900/50 p-6">
        <div className="flex items-center gap-2 text-sm font-medium text-gray-300">
          <span>🔒</span>
          <span>Privacy & Security</span>
        </div>
        <p className="text-xs leading-relaxed text-gray-500">
          Everything runs locally on your machine. Your credentials are encrypted
          and stored in your operating system's secure keychain (Windows DPAPI or
          macOS Keychain). No data is ever sent to external servers — only Twitch
          and GitHub (for update checks).
        </p>
      </div>

      <button
        onClick={onNext}
        className="w-full rounded-lg bg-purple-600 px-4 py-3 text-sm font-semibold text-white hover:bg-purple-700 transition-colors"
      >
        Let's get started →
      </button>
    </div>
  );
}
