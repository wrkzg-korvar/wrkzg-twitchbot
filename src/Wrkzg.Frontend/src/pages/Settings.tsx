import { SettingsAuthSection } from "./SettingsAuthSection";

export function Settings() {
  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-8 text-2xl font-bold">Settings</h1>
      <SettingsAuthSection />
    </div>
  );
}