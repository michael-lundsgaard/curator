import { Check, X } from "lucide-react";
import type { ReactNode } from "react";

// What an action did, until it's dismissed: "Added 3 clips to Summer 2026."
export function Done({ children, onDismiss }: { children: ReactNode; onDismiss: () => void }) {
  return (
    <div
      role="status"
      className="flex items-center gap-2.5 rounded-[10px] bg-ok-soft py-1.5 pr-1.5 pl-4 text-[15px] font-semibold text-ok"
    >
      <Check size={18} aria-hidden="true" className="flex-none" />
      <span className="min-w-0 flex-1 break-words">{children}</span>
      <button
        type="button"
        onClick={onDismiss}
        aria-label="Dismiss"
        title="Dismiss"
        className="flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg hover:bg-ok/10"
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
