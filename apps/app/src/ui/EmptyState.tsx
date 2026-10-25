import type { ReactNode } from "react";

// Nothing to show where a listing or a page would be: no clips yet, no matches, or it doesn't exist.
export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-[14px] border border-dashed border-line px-6 py-16 text-center">
      <span className="text-lg font-semibold">{title}</span>
      {children}
    </div>
  );
}
