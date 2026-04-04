import { useQuery } from "@tanstack/react-query";
import { notificationsApi } from "../api/notifications";
import { PageHeader } from "../components/ui/PageHeader";
import { NotificationCard } from "../components/features/notifications/NotificationCard";
import type { NotificationSettings } from "../types/notifications";

export function NotificationsPage() {
  const { data: settings, isLoading } = useQuery<NotificationSettings>({
    queryKey: ["notificationSettings"],
    queryFn: notificationsApi.getSettings,
  });

  if (isLoading) {
    return (
      <div className="p-6 text-sm text-[var(--color-text-muted)]">
        Loading notifications...
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title="Event Notifications"
        description="Configure chat messages for follows, subs, raids, and more."
        helpKey="notifications"
      />

      <div className="rounded-lg border border-blue-500/20 bg-blue-500/5 p-4 text-sm text-blue-700 dark:text-blue-300">
        <p>
          EventSub connects automatically when your Broadcaster account is linked.
          If notifications are not working, try reconnecting your Broadcaster account in{" "}
          <strong>Settings</strong> to grant updated permissions.
        </p>
      </div>

      <div className="space-y-4">
        {settings &&
          Object.entries(settings).map(([type, setting]) => (
            <NotificationCard key={type} type={type} setting={setting} />
          ))}
      </div>
    </div>
  );
}
