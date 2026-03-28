import { Routes, Route, Navigate } from "react-router-dom";
import { useSetupStatus } from "./hooks/useSetupStatus";
import { DashboardLayout } from "./components/layout/DashboardLayout";
import { SetupWizard } from "./pages/SetupWizard";
import { Dashboard } from "./pages/Dashboard";
import { Commands } from "./pages/Commands";
import { Users } from "./pages/Users";
import { Settings } from "./pages/Settings";
import { Polls } from "./pages/Polls";
import { Raffles } from "./pages/Raffles";
import { Timers } from "./pages/Timers";
import { Counters } from "./pages/Counters";
import { Quotes } from "./pages/Quotes";
import { SpamFilter } from "./pages/SpamFilter";

export default function App() {
  const { setupComplete, isLoading } = useSetupStatus();

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center bg-gray-950 text-gray-400">
        <div className="text-center">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-2 border-gray-600 border-t-purple-500 mx-auto" />
          <p>Loading Wrkzg…</p>
        </div>
      </div>
    );
  }

  // If setup is not complete, show the wizard — no other routes available
  if (!setupComplete) {
    return (
      <Routes>
        <Route path="/setup/*" element={<SetupWizard />} />
        <Route path="*" element={<Navigate to="/setup" replace />} />
      </Routes>
    );
  }

  // Setup complete — show the full dashboard
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<Dashboard />} />
        <Route path="commands" element={<Commands />} />
        <Route path="users" element={<Users />} />
        <Route path="polls" element={<Polls />} />
        <Route path="raffles" element={<Raffles />} />
        <Route path="timers" element={<Timers />} />
        <Route path="counters" element={<Counters />} />
        <Route path="quotes" element={<Quotes />} />
        <Route path="spam-filter" element={<SpamFilter />} />
        <Route path="settings" element={<Settings />} />
      </Route>
      {/* Redirect unknown routes to dashboard */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
