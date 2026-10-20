import { useEffect, useState } from "react";

export type Theme = "light" | "dark";

// The app follows the OS theme (prefers-color-scheme) unless a theme that differs from it is remembered. A toggle
// that lands back on the OS theme forgets the choice, so the app follows the OS again. The choice is per device, so
// it lives in localStorage, not the profile. index.html applies the same rule before the first paint.
const storageKey = "theme";
const darkQuery = "(prefers-color-scheme: dark)";

function osTheme(): Theme {
  return matchMedia(darkQuery).matches ? "dark" : "light";
}

function readStored(): Theme | null {
  try {
    const stored = localStorage.getItem(storageKey);
    return stored === "light" || stored === "dark" ? stored : null;
  } catch {
    return null;
  }
}

function writeStored(theme: Theme | null) {
  try {
    if (theme === null) localStorage.removeItem(storageKey);
    else localStorage.setItem(storageKey, theme);
  } catch {
    // Not remembered, but still applied for this visit.
  }
}

export function useTheme() {
  // null: nothing remembered, so the OS theme applies
  const [stored, setStored] = useState(readStored);
  const [os, setOs] = useState(osTheme);
  const theme = stored ?? os;

  useEffect(() => {
    const query = matchMedia(darkQuery);
    const onChange = () => setOs(osTheme());
    query.addEventListener("change", onChange);
    return () => query.removeEventListener("change", onChange);
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

  function toggle() {
    const next = theme === "dark" ? "light" : "dark";
    const remembered = next === os ? null : next;
    writeStored(remembered);
    setStored(remembered);
  }

  return [theme, toggle] as const;
}
