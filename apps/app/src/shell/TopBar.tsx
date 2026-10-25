import { Link } from "@tanstack/react-router";
import { Upload } from "lucide-react";
import { AccountMenu } from "./AccountMenu";
import { SearchBox } from "./SearchBox";
import { ThemeSwitch } from "./ThemeSwitch";

export function TopBar() {
  return (
    <header className="flex flex-wrap items-center justify-end gap-3 border-b border-line px-4 py-4 md:px-8">
      {/* mr-auto keeps the buttons on the right; on a phone the search box takes a row of its own. */}
      <SearchBox className="mr-auto max-w-140 min-w-0 flex-[1_1_280px]" />
      <Link
        to="/upload"
        className="flex h-11 items-center gap-2 rounded-[10px] bg-accent px-4 text-[15px] font-semibold text-white hover:bg-accent-hover"
      >
        <Upload size={18} />
        Upload
      </Link>
      <ThemeSwitch />
      <AccountMenu />
    </header>
  );
}
