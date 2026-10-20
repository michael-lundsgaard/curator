import { Moon, Sun } from "lucide-react";
import { useTheme } from "../theme";

// One button, as in the mocks: its icon shows the theme a click switches to. See src/theme.ts for when the choice
// is remembered.
export function ThemeSwitch() {
  const [theme, toggle] = useTheme();
  const label = theme === "dark" ? "Switch to light theme" : "Switch to dark theme";

  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={toggle}
      className="flex size-11 cursor-pointer items-center justify-center rounded-[10px] border border-line bg-surface text-text hover:bg-sunken"
    >
      {theme === "dark" ? <Sun size={18} /> : <Moon size={18} />}
    </button>
  );
}
