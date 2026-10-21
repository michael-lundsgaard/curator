import { getRouteApi } from "@tanstack/react-router";
import { LogOut } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { auth } from "../auth/client";

const authenticated = getRouteApi("/_authenticated");

export function AccountMenu() {
  const me = authenticated.useLoaderData();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  // Closes on a click outside the menu or on Escape, like a native menu.
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
        aria-label="Account menu"
        aria-expanded={open}
        onClick={() => setOpen(!open)}
        className="size-11 cursor-pointer rounded-full bg-accent-soft text-sm font-semibold text-link"
      >
        {initials(me.displayName)}
      </button>
      {open && (
        <div className="absolute top-full right-0 z-10 mt-2 flex w-60 flex-col gap-1 rounded-xl border border-line bg-surface p-2 shadow-lg">
          <div className="flex flex-col gap-0.5 px-2.5 py-2">
            <span className="truncate text-[15px] font-semibold">{me.displayName}</span>
            <span className="font-mono text-xs text-meta">{me.role}</span>
          </div>
          <div className="border-t border-line" />
          {/* "local" signs out this browser only; the default would end the user's sessions on every device. */}
          <button
            type="button"
            onClick={() => void auth.signOut({ scope: "local" })}
            className="flex h-10 cursor-pointer items-center gap-2.5 rounded-lg px-2.5 text-[15px] font-semibold hover:bg-sunken"
          >
            <LogOut size={18} />
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}

// "michael.lundsgaard" (the default, from the email address) and "Michael Lundsgaard" both give "ML".
function initials(displayName: string) {
  const words = displayName.split(/[\s._-]+/).filter(Boolean);
  return words
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join("");
}
