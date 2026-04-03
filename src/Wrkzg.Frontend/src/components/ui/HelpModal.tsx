import { Modal } from "./Modal";
import { helpContent } from "../../data/helpContent";

interface HelpModalProps {
  open: boolean;
  onClose: () => void;
  helpKey: string;
}

export function HelpModal({ open, onClose, helpKey }: HelpModalProps) {
  const content = helpContent[helpKey];
  if (!content) {
    return null;
  }

  return (
    <Modal open={open} onClose={onClose} title={content.title} size="lg">
      <div className="max-h-[70vh] overflow-y-auto space-y-5">
        {/* Description */}
        <p className="text-sm text-[var(--color-text-secondary)]">
          {content.description}
        </p>

        {/* How to Use */}
        <div>
          <h4 className="text-sm font-semibold text-[var(--color-text)] mb-2">
            How to use
          </h4>
          <ol className="list-decimal list-inside space-y-1 text-sm text-[var(--color-text-secondary)]">
            {content.howToUse.map((step, i) => (
              <li key={i}>{step}</li>
            ))}
          </ol>
        </div>

        {/* Chat Commands */}
        {content.chatCommands && content.chatCommands.length > 0 && (
          <div>
            <h4 className="text-sm font-semibold text-[var(--color-text)] mb-2">
              Chat Commands
            </h4>
            <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-[var(--color-elevated)]">
                    <th className="text-left px-3 py-2 font-medium text-[var(--color-text-secondary)]">Command</th>
                    <th className="text-left px-3 py-2 font-medium text-[var(--color-text-secondary)]">Description</th>
                    <th className="text-left px-3 py-2 font-medium text-[var(--color-text-secondary)]">Permission</th>
                  </tr>
                </thead>
                <tbody>
                  {content.chatCommands.map((cmd, i) => (
                    <tr key={i} className="border-t border-[var(--color-border)]">
                      <td className="px-3 py-2 font-mono text-xs text-[var(--color-text)]">{cmd.command}</td>
                      <td className="px-3 py-2 text-[var(--color-text-secondary)]">{cmd.description}</td>
                      <td className="px-3 py-2 text-xs text-[var(--color-text-muted)]">{cmd.permission ?? "Everyone"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Template Variables */}
        {content.templateVariables && content.templateVariables.length > 0 && (
          <div>
            <h4 className="text-sm font-semibold text-[var(--color-text)] mb-2">
              Template Variables
            </h4>
            <div className="rounded-lg border border-[var(--color-border)] overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-[var(--color-elevated)]">
                    <th className="text-left px-3 py-2 font-medium text-[var(--color-text-secondary)]">Variable</th>
                    <th className="text-left px-3 py-2 font-medium text-[var(--color-text-secondary)]">Description</th>
                  </tr>
                </thead>
                <tbody>
                  {content.templateVariables.map((v, i) => (
                    <tr key={i} className="border-t border-[var(--color-border)]">
                      <td className="px-3 py-2 font-mono text-xs text-[var(--color-brand-text)]">{v.variable}</td>
                      <td className="px-3 py-2 text-[var(--color-text-secondary)]">{v.description}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Handbook Link */}
        {content.handbookSection && (
          <p className="text-xs text-[var(--color-text-muted)] pt-2 border-t border-[var(--color-border)]">
            For more details, see the{" "}
            <a
              href={`https://github.com/wrkzg-korvar/wrkzg-twitchbot/blob/main/_docs/HANDBOOK.md${content.handbookSection}`}
              target="_blank"
              rel="noopener noreferrer"
              className="underline hover:text-[var(--color-text)]"
            >
              User Handbook
            </a>
          </p>
        )}
      </div>
    </Modal>
  );
}
