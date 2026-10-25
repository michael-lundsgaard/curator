import { Link } from "@tanstack/react-router";
import { useState, type ReactNode } from "react";
import { Folder, House, LayoutGrid, Plus, Upload } from "lucide-react";
import { NewCollection } from "../collections/NewCollection";
import { flatten, useCollectionTree } from "../collections/tree";
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
      <Collections />
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

// The whole tree, every level open. On a phone, where the sidebar sits above the page, it scrolls in a box of its
// own instead of pushing the page down, until the phone navigation is reworked (see the roadmap).
function Collections() {
  const tree = useCollectionTree();
  const [creating, setCreating] = useState(false);

  return (
    <div className="flex flex-col gap-0.5">
      <div className="flex items-center justify-between pb-0.5 pl-2.5">
        <h2 className="font-mono text-xs font-normal tracking-[0.08em] text-meta uppercase">
          Collections
        </h2>
        <button
          type="button"
          onClick={() => setCreating(true)}
          aria-label="New collection"
          title="New collection"
          className="flex size-8 cursor-pointer items-center justify-center rounded-lg text-meta hover:bg-sunken hover:text-text"
        >
          <Plus size={16} aria-hidden="true" />
        </button>
      </div>
      {creating && <NewCollection parent={null} onClose={() => setCreating(false)} />}
      {tree.isError ? (
        <p className="px-2.5 text-sm text-danger">Couldn't load the collections.</p>
      ) : tree.data?.items.length === 0 ? (
        <p className="px-2.5 text-sm text-meta">None yet.</p>
      ) : (
        <ul className="flex max-h-64 flex-col gap-0.5 overflow-y-auto md:max-h-none md:overflow-visible">
          {flatten(tree.data?.items ?? []).map(({ item, depth }) => (
            <li key={item.id}>
              <Link
                to="/collections/$id"
                params={{ id: item.id }}
                // 18px more per level, so the tree reads at a glance; the depth is at most 5.
                style={{ paddingLeft: 10 + depth * 18 }}
                className="flex h-9 items-center gap-2 rounded-lg pr-2.5 text-sm text-text hover:bg-sunken aria-[current=page]:bg-accent-soft aria-[current=page]:font-semibold aria-[current=page]:text-link"
              >
                <Folder size={16} aria-hidden="true" className="flex-none" />
                <span className="min-w-0 flex-1 truncate">{item.name}</span>
                <span className="font-mono text-xs font-normal text-meta">{item.clipCount}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
