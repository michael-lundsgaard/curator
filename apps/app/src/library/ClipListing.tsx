import { keepPreviousData } from "@tanstack/react-query";
import { getRouteApi } from "@tanstack/react-router";
import { useState, type ReactNode } from "react";
import { $api } from "../api/client";
import type { operations } from "../api/schema";
import { Done, LoadError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { ClipGrid, type Clip } from "./ClipGrid";
import { ClipTable } from "./ClipTable";
import { SelectionBar } from "./SelectionBar";
import type { ClipView } from "./view";

export type ClipQuery = Omit<
  NonNullable<operations["ListAssets"]["parameters"]["query"]>,
  "cursor"
>;

const authenticated = getRouteApi("/_authenticated");

const noneSelected = new Set<string>();

// The clips of GET /assets for a query, as a grid or a list, with "Load more" for the next page. The library and a
// collection's page show their clips with it; `empty` is what to show when there are none. Clips can be selected to add
// them to a collection, or remove them from `collection`, the one whose page it is.
export function ClipListing({
  query,
  view,
  empty,
  collection,
}: {
  query: ClipQuery;
  view: ClipView;
  empty: ReactNode;
  collection?: { id: string; name: string };
}) {
  const me = authenticated.useLoaderData();
  // A selection belongs to the query it was made in: another search or filter starts with none, so no clip is selected
  // out of sight.
  const queryKey = JSON.stringify(query);
  const [selection, setSelection] = useState({ queryKey, ids: noneSelected });
  const selected = selection.queryKey === queryKey ? selection.ids : noneSelected;
  const [done, setDone] = useState<ReactNode>(null);

  const toggle = (id: string) => {
    const ids = new Set(selected);
    if (!ids.delete(id)) ids.add(id);
    setSelection({ queryKey, ids });
    setDone(null);
  };
  const clear = () => setSelection({ queryKey, ids: noneSelected });

  const clips = $api.useInfiniteQuery(
    "get",
    "/assets",
    { params: { query } },
    {
      // The first page has no cursor: openapi-fetch leaves null out of the query string.
      initialPageParam: null,
      getNextPageParam: (page) => page.nextCursor,
      // Keeps the clips on screen while another search or filter loads, instead of flashing "Loading".
      placeholderData: keepPreviousData,
      // A 404 (an unknown collection) won't go away on a retry.
      retry: (count, error) => error.status !== 404 && count < 3,
    },
  );

  const items = clips.data?.pages.flatMap((page) => page.items) ?? [];
  const uploaderName = (clip: Clip) =>
    clip.uploadedBy.id === me.id ? "You" : clip.uploadedBy.displayName;

  // All the clips loaded so far, not the pages "Load more" hasn't fetched: adding every clip a search finds would
  // need the API to take the search instead of ids.
  const allSelected = items.length > 0 && items.every((clip) => selected.has(clip.id));
  const selectAll = () => {
    setSelection({ queryKey, ids: new Set(items.map((clip) => clip.id)) });
    setDone(null);
  };
  const toggleAll = () => (allSelected ? clear() : selectAll());

  return (
    <>
      {(selected.size > 0 || done) && (
        // Stays at the top of the screen, so the selection can be added after scrolling down to "Load more".
        <div className="sticky top-3 z-10">
          {selected.size > 0 ? (
            <SelectionBar
              assetIds={[...selected]}
              collection={collection}
              includeSubCollections={query.includeSubCollections}
              onDone={(message) => {
                clear();
                setDone(message);
              }}
              onSelectAll={allSelected ? undefined : selectAll}
              onClear={clear}
            />
          ) : (
            <Done onDismiss={() => setDone(null)}>{done}</Done>
          )}
        </div>
      )}

      {clips.isPending ? (
        <p className="text-[15px] text-meta">Loading clips…</p>
      ) : clips.isError && items.length === 0 ? (
        <LoadError onRetry={() => void clips.refetch()}>Couldn't load the clips.</LoadError>
      ) : items.length === 0 ? (
        empty
      ) : (
        // Dimmed while another search or filter loads over the current clips.
        <div
          aria-busy={clips.isPlaceholderData}
          className={clips.isPlaceholderData ? "opacity-60" : undefined}
        >
          {view === "list" ? (
            <ClipTable
              clips={items}
              uploaderName={uploaderName}
              selected={selected}
              onToggle={toggle}
              onToggleAll={toggleAll}
            />
          ) : (
            <ClipGrid
              clips={items}
              uploaderName={uploaderName}
              selected={selected}
              onToggle={toggle}
            />
          )}
        </div>
      )}

      {clips.hasNextPage && !clips.isPlaceholderData && (
        <Button
          size="lg"
          onClick={() => void clips.fetchNextPage()}
          disabled={clips.isFetchingNextPage}
          className="self-center"
        >
          {clips.isFetchingNextPage ? "Loading…" : "Load more"}
        </Button>
      )}
      {clips.isFetchNextPageError && (
        <p role="alert" className="self-center text-[15px] text-danger">
          Couldn't load more clips. Try again.
        </p>
      )}
    </>
  );
}
