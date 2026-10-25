import type { ReactNode } from "react";

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
