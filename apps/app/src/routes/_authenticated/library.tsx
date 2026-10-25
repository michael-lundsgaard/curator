import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, getRouteApi, Link } from "@tanstack/react-router";
import { LayoutGrid, List } from "lucide-react";
import type { ReactNode } from "react";
import { $api } from "../../api/client";
import type { components } from "../../api/schema";
import { ClipGrid, type Clip } from "../../library/ClipGrid";
import { ClipTable } from "../../library/ClipTable";
import { useClipView } from "../../library/view";

type AssetSort = components["schemas"]["AssetSort"];
type SortOrder = components["schemas"]["SortOrder"];

// What the library shows lives in the URL, so a search can be linked to and Back undoes a filter. Missing means the
// API's default; the top bar's search box sets q. How it's shown (grid or list) is a preference, see view.ts.
type LibrarySearch = {
  q?: string;
  sort?: AssetSort;
  order?: SortOrder;
  uploadedBy?: string;
};

const sorts: { sort: AssetSort; order: SortOrder; label: string }[] = [
  { sort: "Recorded", order: "Desc", label: "Recorded, newest first" },
  { sort: "Recorded", order: "Asc", label: "Recorded, oldest first" },
  { sort: "Uploaded", order: "Desc", label: "Uploaded, newest first" },
  { sort: "Uploaded", order: "Asc", label: "Uploaded, oldest first" },
  { sort: "Title", order: "Asc", label: "Title, A to Z" },
  { sort: "Title", order: "Desc", label: "Title, Z to A" },
];

export const Route = createFileRoute("/_authenticated/library")({
  validateSearch: (search): LibrarySearch => {
    const sort = sorts.find((s) => s.sort === search.sort)?.sort;
    const order =
      sort && (search.order === "Asc" || search.order === "Desc") ? search.order : undefined;
    return {
      q: typeof search.q === "string" && search.q.trim() ? search.q : undefined,
      sort,
      order,
      uploadedBy: typeof search.uploadedBy === "string" ? search.uploadedBy : undefined,
    };
  },
  component: Library,
});

const authenticated = getRouteApi("/_authenticated");

const selectClass = "h-10 rounded-lg border border-line bg-surface px-2.5 text-sm text-text";
const buttonClass =
  "flex h-11 cursor-pointer items-center rounded-[10px] border border-line bg-surface px-5 text-[15px] font-semibold hover:bg-sunken disabled:cursor-default disabled:opacity-60";

function Library() {
  const search = Route.useSearch();
  const navigate = Route.useNavigate();
  const me = authenticated.useLoaderData();
  const [view, setView] = useClipView();
  const profiles = $api.useQuery("get", "/profiles");

  const clips = $api.useInfiniteQuery(
    "get",
    "/assets",
    {
      params: {
        query: {
          q: search.q,
          sort: search.sort,
          order: search.order,
          uploadedBy: search.uploadedBy,
        },
      },
    },
    {
      // The first page has no cursor: openapi-fetch leaves null out of the query string.
      initialPageParam: null,
      getNextPageParam: (page) => page.nextCursor,
      // Keeps the clips on screen while another search or filter loads, instead of flashing "Loading".
      placeholderData: keepPreviousData,
    },
  );

  const items = clips.data?.pages.flatMap((page) => page.items) ?? [];
  const filtered = search.q !== undefined || search.uploadedBy !== undefined;
  const uploaderName = (clip: Clip) =>
    clip.uploadedBy.id === me.id ? "You" : clip.uploadedBy.displayName;

  const sort = search.sort ?? "Recorded";
  const order = search.order ?? (sort === "Title" ? "Asc" : "Desc");

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-1.5">
          <h1 className="text-[34px] font-extrabold tracking-[-0.02em]">Library</h1>
          {search.q && <p className="text-[15px] text-meta">Clips matching “{search.q}”</p>}
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <label className="flex items-center gap-2 text-sm text-meta">
            Uploaded by
            <select
              value={search.uploadedBy ?? ""}
              onChange={(e) =>
                void navigate({
                  search: (prev) => ({ ...prev, uploadedBy: e.target.value || undefined }),
                })
              }
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
          <label className="flex items-center gap-2 text-sm text-meta">
            Sort
            <select
              value={`${sort}-${order}`}
              onChange={(e) => {
                const choice = sorts.find((s) => `${s.sort}-${s.order}` === e.target.value)!;
                // The default sort stays out of the URL.
                const isDefault = choice === sorts[0];
                void navigate({
                  search: (prev) => ({
                    ...prev,
                    sort: isDefault ? undefined : choice.sort,
                    order: isDefault ? undefined : choice.order,
                  }),
                });
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
          <div
            role="group"
            aria-label="View"
            className="flex overflow-hidden rounded-lg border border-line"
          >
            <ViewButton label="Grid view" pressed={view === "grid"} onClick={() => setView("grid")}>
              <LayoutGrid size={16} />
            </ViewButton>
            <ViewButton label="List view" pressed={view === "list"} onClick={() => setView("list")}>
              <List size={16} />
            </ViewButton>
          </div>
        </div>
      </div>

      {clips.isPending ? (
        <p className="text-[15px] text-meta">Loading clips…</p>
      ) : clips.isError && items.length === 0 ? (
        <div
          role="alert"
          className="flex flex-wrap items-center gap-3 rounded-[10px] bg-danger-soft px-4 py-3 text-danger"
        >
          <span className="flex-1 text-[15px] font-semibold">Couldn't load the clips.</span>
          <button
            type="button"
            onClick={() => void clips.refetch()}
            className="cursor-pointer font-semibold underline"
          >
            Try again
          </button>
        </div>
      ) : items.length === 0 ? (
        filtered ? (
          <EmptyState
            title={search.q ? `No clips match “${search.q}”` : "No clips match this filter"}
          >
            {search.q && (
              <span className="text-[15px] text-meta">
                Search looks for every word in titles and descriptions.
              </span>
            )}
            <Link
              to="/library"
              search={{ sort: search.sort, order: search.order }}
              className={buttonClass}
            >
              Show all clips
            </Link>
          </EmptyState>
        ) : (
          <EmptyState title="No clips yet">
            <span className="text-[15px] text-meta">
              Upload a few, and they show up here for everyone.
            </span>
            <Link to="/upload" className={buttonClass}>
              Upload clips
            </Link>
          </EmptyState>
        )
      ) : (
        // Dimmed while another search or filter loads over the current clips.
        <div
          aria-busy={clips.isPlaceholderData}
          className={clips.isPlaceholderData ? "opacity-60" : undefined}
        >
          {view === "list" ? (
            <ClipTable clips={items} uploaderName={uploaderName} />
          ) : (
            <ClipGrid clips={items} uploaderName={uploaderName} />
          )}
        </div>
      )}

      {clips.hasNextPage && !clips.isPlaceholderData && (
        <button
          type="button"
          onClick={() => void clips.fetchNextPage()}
          disabled={clips.isFetchingNextPage}
          className={`${buttonClass} self-center`}
        >
          {clips.isFetchingNextPage ? "Loading…" : "Load more"}
        </button>
      )}
      {clips.isFetchNextPageError && (
        <p role="alert" className="self-center text-[15px] text-danger">
          Couldn't load more clips. Try again.
        </p>
      )}
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

function EmptyState({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-[14px] border border-dashed border-line px-6 py-16 text-center">
      <span className="text-lg font-semibold">{title}</span>
      {children}
    </div>
  );
}
