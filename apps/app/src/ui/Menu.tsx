import { useEffect, useRef, useState, type ReactNode } from "react";

// A button that opens a list of actions below it. Closes on a click outside, on Escape or when an item is chosen, like
// a native menu.
export function Menu({
  label,
  button,
  buttonClassName,
  children,
}: {
  label: string;
  button: ReactNode;
  buttonClassName: string;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      if (!ref.current?.contains(event.target as Node)) setOpen(false);
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        aria-label={label}
        title={label}
        aria-expanded={open}
        onClick={() => setOpen(!open)}
        className={buttonClassName}
      >
        {button}
      </button>
      {open && (
        // A click on an item bubbles up here and closes the menu after the item's own onClick.
        <div
          onClick={() => setOpen(false)}
          className="absolute top-full right-0 z-10 mt-2 flex w-64 flex-col gap-1 rounded-xl border border-line bg-surface p-2 shadow-lg"
        >
          {children}
        </div>
      )}
    </div>
  );
}

export function MenuItem({
  onClick,
  icon,
  hint,
  danger = false,
  disabled = false,
  children,
}: {
  onClick: () => void;
  icon?: ReactNode;
  // A line below the label: what the action also does, or why it's unavailable.
  hint?: string;
  danger?: boolean;
  disabled?: boolean;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`flex min-h-10 cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2 text-left text-[15px] font-semibold hover:bg-sunken disabled:cursor-default disabled:opacity-60 disabled:hover:bg-transparent ${danger ? "text-danger" : "text-text"}`}
    >
      {icon}
      <span className="flex flex-col gap-0.5">
        {children}
        {hint && <span className="text-[13px] font-normal text-meta">{hint}</span>}
      </span>
    </button>
  );
}

export function MenuDivider() {
  return <div className="border-t border-line" />;
}
