import { Routes, Route, Navigate, useLocation } from "react-router-dom";
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
import { OverlaysPage } from "./pages/OverlaysPage";
import { ChannelPointsPage } from "./pages/ChannelPointsPage";
import { RolesPage } from "./pages/RolesPage";
import { ChatGamesPage } from "./pages/ChatGamesPage";
import { AnalyticsPage } from "./pages/AnalyticsPage";
import { AlertOverlay } from "./components/overlay/AlertOverlay";
import { ChatOverlay } from "./components/overlay/ChatOverlay";
import { PollOverlay } from "./components/overlay/PollOverlay";
import { RaffleOverlay } from "./components/overlay/RaffleOverlay";
import { CounterOverlay } from "./components/overlay/CounterOverlay";
import { EventListOverlay } from "./components/overlay/EventListOverlay";

export default function App() {
  const location = useLocation();

  // Overlay routes bypass ALL app logic (no setup check, no auth, no layout)
  // They run standalone in OBS Browser Sources
  if (location.pathname.startsWith("/overlay/")) {
    return (
      <Routes>
        <Route path="/overlay/alerts" element={<AlertOverlay />} />
        <Route path="/overlay/chat" element={<ChatOverlay />} />
        <Route path="/overlay/poll" element={<PollOverlay />} />
        <Route path="/overlay/raffle" element={<RaffleOverlay />} />
        <Route path="/overlay/counter" element={<CounterOverlay />} />
        <Route path="/overlay/events" element={<EventListOverlay />} />
      </Routes>
    );
  }

  return <AppShell />;
}

function AppShell() {
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

  if (!setupComplete) {
    return (
      <Routes>
        <Route path="/setup/*" element={<SetupWizardPage />} />
        <Route path="*" element={<Navigate to="/setup" replace />} />
      </Routes>
    );
  }

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
          <Route path="channel-points" element={<ChannelPointsPage />} />
          <Route path="roles" element={<RolesPage />} />
          <Route path="games" element={<ChatGamesPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="quotes" element={<QuotesPage />} />
          <Route path="spam-filter" element={<SpamFilterPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="overlays" element={<OverlaysPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      <ToastContainer />
    </>
  );
}
