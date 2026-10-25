import { getRouteApi } from "@tanstack/react-router";
import { LogOut } from "lucide-react";
import { auth } from "../auth/client";
import { Menu, MenuDivider, MenuItem } from "../ui/Menu";

const authenticated = getRouteApi("/_authenticated");

export function AccountMenu() {
  const me = authenticated.useLoaderData();

  return (
    <Menu
      label="Account menu"
      button={initials(me.displayName)}
      buttonClassName="size-11 cursor-pointer rounded-full bg-accent-soft text-sm font-semibold text-link"
    >
      <div className="flex flex-col gap-0.5 px-2.5 py-2">
        <span className="truncate text-[15px] font-semibold">{me.displayName}</span>
        <span className="font-mono text-xs text-meta">{me.role}</span>
      </div>
      <MenuDivider />
      {/* "local" signs out this browser only; the default would end the user's sessions on every device. */}
      <MenuItem onClick={() => void auth.signOut({ scope: "local" })} icon={<LogOut size={18} />}>
        Sign out
      </MenuItem>
    </Menu>
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
