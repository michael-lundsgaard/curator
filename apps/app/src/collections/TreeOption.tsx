import type { ReactNode } from "react";

// The collection tree as a list of radio buttons, for picking where a collection moves or where clips go. Scrolls
// when the tree is long.
export function TreeOptions({ children }: { children: ReactNode }) {
  return (
    <div className="flex max-h-80 flex-col gap-0.5 overflow-y-auto rounded-[10px] border border-line p-1.5">
      {children}
    </div>
  );
}

export function TreeOption({
  checked,
  onChange,
  depth,
  disabled = false,
  icon,
  children,
}: {
  checked: boolean;
  onChange: () => void;
  depth: number;
  disabled?: boolean;
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    <label
      // Indented like the sidebar.
      style={{ paddingLeft: 10 + depth * 18 }}
      className="flex min-h-10 cursor-pointer items-center gap-2 rounded-lg pr-2.5 text-sm hover:bg-sunken has-checked:bg-accent-soft has-checked:font-semibold has-checked:text-link has-disabled:cursor-default has-disabled:text-meta has-disabled:hover:bg-transparent"
    >
      <input
        type="radio"
        name="collection"
        checked={checked}
        onChange={onChange}
        disabled={disabled}
        className="size-4 flex-none accent-accent"
      />
      <span className="flex-none" aria-hidden="true">
        {icon}
      </span>
      <span className="min-w-0 truncate">{children}</span>
    </label>
  );
}
