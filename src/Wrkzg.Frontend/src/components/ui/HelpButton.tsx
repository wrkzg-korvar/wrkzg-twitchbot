import { CircleHelp } from "lucide-react";
import { useState } from "react";
import { HelpModal } from "./HelpModal";

interface HelpButtonProps {
  helpKey: string;
}

export function HelpButton({ helpKey }: HelpButtonProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setIsOpen(true)}
        className="text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors"
        title="Help"
        aria-label="Show help for this page"
      >
        <CircleHelp className="h-[18px] w-[18px]" />
      </button>
      <HelpModal
        open={isOpen}
        onClose={() => setIsOpen(false)}
        helpKey={helpKey}
      />
    </>
  );
}
