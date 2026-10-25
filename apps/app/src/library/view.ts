import { useState } from "react";

export type ClipView = "grid" | "list";

// Grid or list is a preference for this device, like the theme, not part of what's being looked at: it stays out of
// the URL, so a shared link shows clips the way the person opening it likes them. One choice for every clip listing.
const storageKey = "clip-view";

function readStored(): ClipView {
  try {
    return localStorage.getItem(storageKey) === "list" ? "list" : "grid";
  } catch {
    return "grid";
  }
}

export function useClipView() {
  const [view, setView] = useState(readStored);

  function choose(next: ClipView) {
    setView(next);
    try {
      localStorage.setItem(storageKey, next);
    } catch {
      // Not remembered, but still shown for this visit.
    }
  }

  return [view, choose] as const;
}
