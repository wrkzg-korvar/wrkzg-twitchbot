import { useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { WelcomeStep } from "../components/wizard/WelcomeStep";
import { TwitchAppStep } from "../components/wizard/TwitchAppStep";
import { BotConnectStep } from "../components/wizard/BotConnectStep";
import { BroadcasterConnectStep } from "../components/wizard/BroadcasterConnectStep";
import { ChannelStep } from "../components/wizard/ChannelStep";
import { TitleBar } from "../components/layout/TitleBar";

const STEPS = [
  { id: "welcome", label: "Welcome" },
  { id: "twitch-app", label: "Twitch App" },
  { id: "bot", label: "Bot Account" },
  { id: "broadcaster", label: "Broadcaster" },
  { id: "channel", label: "Channel" },
] as const;

export function SetupWizardPage() {
  const [currentStep, setCurrentStep] = useState(0);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const goNext = useCallback(() => {
    if (currentStep < STEPS.length - 1) {
      setCurrentStep((s) => s + 1);
    }
  }, [currentStep]);

  const goBack = useCallback(() => {
    if (currentStep > 0) {
      setCurrentStep((s) => s - 1);
    }
  }, [currentStep]);

  const finishSetup = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: ["setupStatus"] });
    navigate("/", { replace: true });
  }, [queryClient, navigate]);

  return (
    <div className="flex h-full flex-col bg-[var(--color-bg)]">
      <TitleBar />

      <div className="border-b border-[var(--color-border)] bg-[var(--color-bg)]/80 backdrop-blur">
        <div className="mx-auto flex max-w-2xl items-center gap-1 px-6 py-4">
          {STEPS.map((step, i) => (
            <div key={step.id} className="flex items-center gap-1">
              <div
                className={`flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold transition-colors ${
                  i < currentStep
                    ? "bg-[var(--color-brand)] text-white"
                    : i === currentStep
                      ? "bg-[var(--color-brand)]/20 text-[var(--color-brand-text)] ring-2 ring-[var(--color-brand)]"
                      : "bg-[var(--color-elevated)] text-[var(--color-text-muted)]"
                }`}
              >
                {i < currentStep ? "✓" : i + 1}
              </div>
              <span
                className={`hidden text-xs sm:inline ${
                  i === currentStep ? "text-[var(--color-brand-text)] font-medium" : "text-[var(--color-text-muted)]"
                }`}
              >
                {step.label}
              </span>
              {i < STEPS.length - 1 && (
                <div
                  className={`mx-1 h-px w-6 ${
                    i < currentStep ? "bg-[var(--color-brand)]" : "bg-[var(--color-elevated)]"
                  }`}
                />
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="flex flex-1 items-start justify-center overflow-y-auto px-6 py-10">
        <div className="w-full max-w-xl">
          {currentStep === 0 && <WelcomeStep onNext={goNext} />}
          {currentStep === 1 && <TwitchAppStep onNext={goNext} onBack={goBack} />}
          {currentStep === 2 && <BotConnectStep onNext={goNext} onBack={goBack} />}
          {currentStep === 3 && <BroadcasterConnectStep onNext={goNext} onBack={goBack} />}
          {currentStep === 4 && <ChannelStep onBack={goBack} onFinish={finishSetup} />}
        </div>
      </div>
    </div>
  );
}
