import { getRouteApi } from "@tanstack/react-router";
import { LayoutGrid, List } from "lucide-react";
import type { ReactNode } from "react";
import { $api } from "../api/client";
import { sorts, type AssetSort, type SortOrder } from "./search";
import type { ClipView } from "./view";

const authenticated = getRouteApi("/_authenticated");

const selectClass = "h-10 rounded-lg border border-line bg-surface px-2.5 text-sm text-text";

// Who uploaded the clips: anyone, you, or one of the others by name. Reports "anyone" as undefined.
export function UploadedBySelect({
  value,
  onChange,
}: {
  value?: string;
  onChange: (uploadedBy?: string) => void;
}) {
  const me = authenticated.useLoaderData();
  // Fresh for a minute, so moving between the library and collections doesn't fetch it each time.
  const profiles = $api.useQuery("get", "/profiles", {}, { staleTime: 60_000 });

  return (
    <label className="flex items-center gap-2 text-sm text-meta">
      Uploaded by
      <select
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value || undefined)}
        className={selectClass}
      >
        <option value="">Anyone</option>
        <option value={me.id}>You</option>
        {profiles.data?.items
          .filter((profile) => profile.id !== me.id)
          .map((profile) => (
            <option key={profile.id} value={profile.id}>
              {profile.displayName}
            </option>
          ))}
      </select>
    </label>
  );
}

// The sort menu of a clip listing. It reports the default sort as undefined, so the default stays out of the URL.
export function SortSelect({
  sort,
  order,
  onChange,
}: {
  sort?: AssetSort;
  order?: SortOrder;
  onChange: (sort?: AssetSort, order?: SortOrder) => void;
}) {
  const current = sort ?? "Recorded";
  const currentOrder = order ?? (current === "Title" ? "Asc" : "Desc");

  return (
    <label className="flex items-center gap-2 text-sm text-meta">
      Sort
      <select
        value={`${current}-${currentOrder}`}
        onChange={(e) => {
          const choice = sorts.find((s) => `${s.sort}-${s.order}` === e.target.value)!;
          if (choice === sorts[0]) onChange(undefined, undefined);
          else onChange(choice.sort, choice.order);
        }}
        className={selectClass}
      >
        {sorts.map((s) => (
          <option key={`${s.sort}-${s.order}`} value={`${s.sort}-${s.order}`}>
            {s.label}
          </option>
        ))}
      </select>
    </label>
  );
}

export function ViewSwitch({
  view,
  onChange,
}: {
  view: ClipView;
  onChange: (view: ClipView) => void;
}) {
  return (
    <div
      role="group"
      aria-label="View"
      className="flex overflow-hidden rounded-lg border border-line"
    >
      <ViewButton label="Grid view" pressed={view === "grid"} onClick={() => onChange("grid")}>
        <LayoutGrid size={16} />
      </ViewButton>
      <ViewButton label="List view" pressed={view === "list"} onClick={() => onChange("list")}>
        <List size={16} />
      </ViewButton>
    </div>
  );
}

function ViewButton({
  label,
  pressed,
  onClick,
  children,
}: {
  label: string;
  pressed: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      aria-pressed={pressed}
      title={label}
      className="flex h-9.5 w-10 cursor-pointer items-center justify-center bg-surface text-text not-first:border-l not-first:border-line hover:bg-sunken aria-pressed:bg-sunken"
    >
      {children}
    </button>
  );
}
