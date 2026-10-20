import { Link } from "@tanstack/react-router";
import { Upload } from "lucide-react";
import { ThemeSwitch } from "./ThemeSwitch";

// Search comes at the Library step and the account menu at sign-in.
export function TopBar() {
  return (
    <header className="flex flex-wrap items-center justify-end gap-3 border-b border-line px-4 py-4 md:px-8">
      <Link
        to="/upload"
        className="flex h-11 items-center gap-2 rounded-[10px] bg-accent px-4 text-[15px] font-semibold text-white hover:bg-accent-hover"
      >
        <Upload size={18} />
        Upload
      </Link>
      <ThemeSwitch />
    </header>
  );
}
