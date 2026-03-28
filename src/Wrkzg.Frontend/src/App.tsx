import { Routes, Route, Navigate } from "react-router-dom";
import { useSetupStatus } from "./hooks/useSetupStatus";
import { DashboardLayout } from "./components/layout/DashboardLayout";
import { ToastContainer } from "./components/ui/Toast";
import { SetupWizardPage } from "./pages/SetupWizardPage";
import { DashboardPage } from "./pages/DashboardPage";
import { CommandsPage } from "./pages/CommandsPage";
import { UsersPage } from "./pages/UsersPage";
import { SettingsPage } from "./pages/SettingsPage";
import { PollsPage } from "./pages/PollsPage";
import { RafflesPage } from "./pages/RafflesPage";
import { TimersPage } from "./pages/TimersPage";
import { CountersPage } from "./pages/CountersPage";
import { QuotesPage } from "./pages/QuotesPage";
import { SpamFilterPage } from "./pages/SpamFilterPage";
import { NotificationsPage } from "./pages/NotificationsPage";

export default function App() {
  const { setupComplete, isLoading } = useSetupStatus();

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center bg-gray-950 text-gray-400">
        <div className="text-center">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-2 border-gray-600 border-t-purple-500 mx-auto" />
          <p>Loading Wrkzg...</p>
        </div>
      </div>
    );
  }

  // If setup is not complete, show the wizard -- no other routes available
  if (!setupComplete) {
    return (
      <Routes>
        <Route path="/setup/*" element={<SetupWizardPage />} />
        <Route path="*" element={<Navigate to="/setup" replace />} />
      </Routes>
    );
  }

  // Setup complete -- show the full dashboard
  return (
    <>
      <Routes>
        <Route element={<DashboardLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="commands" element={<CommandsPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="polls" element={<PollsPage />} />
          <Route path="raffles" element={<RafflesPage />} />
          <Route path="timers" element={<TimersPage />} />
          <Route path="counters" element={<CountersPage />} />
          <Route path="quotes" element={<QuotesPage />} />
          <Route path="spam-filter" element={<SpamFilterPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
        {/* Redirect unknown routes to dashboard */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      <ToastContainer />
    </>
  );
}
