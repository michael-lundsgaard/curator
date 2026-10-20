import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { House, LayoutGrid, Upload } from "lucide-react";
import { Logo } from "./Logo";

export function Sidebar() {
  return (
    // A column beside the page from md up; above it, at full width, on a phone.
    <nav
      aria-label="Main"
      className="flex shrink-0 flex-col gap-6 border-b border-line bg-surface px-3.5 py-5 md:w-62 md:border-r md:border-b-0"
    >
      <Link to="/" className="flex items-center gap-2.5 px-2.5 text-text">
        <Logo />
        <span className="text-[21px] font-extrabold tracking-[-0.03em]">Curator</span>
      </Link>
      <div className="flex flex-col gap-0.5">
        <NavItem to="/" icon={<House size={18} />}>
          Home
        </NavItem>
        <NavItem to="/library" icon={<LayoutGrid size={18} />}>
          Library
        </NavItem>
        <NavItem to="/upload" icon={<Upload size={18} />}>
          Upload
        </NavItem>
      </div>
    </nav>
  );
}

function NavItem({
  to,
  icon,
  children,
}: {
  to: "/" | "/library" | "/upload";
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    // Link sets aria-current="page" on the current route, which the aria-[current=page] classes style.
    // exact: "/" would otherwise count as active on every page.
    <Link
      to={to}
      activeOptions={{ exact: to === "/" }}
      className="flex h-10 items-center gap-2.5 rounded-lg px-2.5 text-[15px] font-semibold text-text hover:bg-sunken aria-[current=page]:bg-accent-soft aria-[current=page]:text-link"
    >
      {icon}
      {children}
    </Link>
  );
}
