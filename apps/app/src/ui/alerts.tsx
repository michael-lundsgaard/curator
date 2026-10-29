import { Check, Info, X } from "lucide-react";
import type { ReactNode } from "react";

// What an action did, until it's dismissed: "Added 3 clips to Summer 2026." In plain colors when it changed nothing
// ("The clip was already in Screams."), so it doesn't read as a success.
export function Done({
  children,
  changed = true,
  onDismiss,
}: {
  children: ReactNode;
  changed?: boolean;
  onDismiss: () => void;
}) {
  const Icon = changed ? Check : Info;
  return (
    <div
      role="status"
      className={`flex items-center gap-2.5 rounded-[10px] py-1.5 pr-1.5 pl-4 text-[15px] font-semibold ${changed ? "bg-ok-soft text-ok" : "bg-sunken text-text"}`}
    >
      <Icon size={18} aria-hidden="true" className="flex-none" />
      <span className="min-w-0 flex-1 break-words">{children}</span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Dismiss"
        title="Dismiss"
        className={`flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg ${changed ? "hover:bg-ok/10" : "hover:bg-text/10"}`}
      >
        <X size={16} aria-hidden="true" />
      </button>
    </div>
  );
}

// A page or a part of one that couldn't load, with a button that tries again: "Couldn't load the clips."
export function LoadError({ children, onRetry }: { children: ReactNode; onRetry: () => void }) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center gap-3 rounded-[10px] bg-danger-soft px-4 py-3 text-danger"
    >
      <span className="flex-1 text-[15px] font-semibold">{children}</span>
      <button type="button" onClick={onRetry} className="cursor-pointer font-semibold underline">
        Try again
      </button>
    </div>
  );
}

// Why a form wasn't saved or sent, above its buttons or fields.
export function FormError({ children }: { children: ReactNode }) {
  return (
    <p role="alert" className="rounded-[10px] bg-danger-soft px-3 py-2.5 text-sm text-danger">
      {children}
    </p>
  );
}
