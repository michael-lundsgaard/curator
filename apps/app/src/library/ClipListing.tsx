import { keepPreviousData } from "@tanstack/react-query";
import { getRouteApi } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { $api } from "../api/client";
import type { operations } from "../api/schema";
import { LoadError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { ClipGrid, type Clip } from "./ClipGrid";
import { ClipTable } from "./ClipTable";
import type { ClipView } from "./view";

export type ClipQuery = Omit<
  NonNullable<operations["ListAssets"]["parameters"]["query"]>,
  "cursor"
>;

const authenticated = getRouteApi("/_authenticated");

// The clips of GET /assets for a query, as a grid or a list, with "Load more" for the next page. The library and a
// collection's page show their clips with it; `empty` is what to show when there are none.
export function ClipListing({
  query,
  view,
  empty,
}: {
  query: ClipQuery;
  view: ClipView;
  empty: ReactNode;
}) {
  const me = authenticated.useLoaderData();
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

  return (
    <>
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
            <ClipTable clips={items} uploaderName={uploaderName} />
          ) : (
            <ClipGrid clips={items} uploaderName={uploaderName} />
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
