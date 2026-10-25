import { createFileRoute, getRouteApi, Link } from "@tanstack/react-router";
import { Folder } from "lucide-react";
import { Fragment } from "react";
import { $api } from "../../api/client";
import type { components } from "../../api/schema";
import {
  ancestorsOf,
  childrenOf,
  useCollectionTree,
  type CollectionItem,
} from "../../collections/tree";
import { buttonClass, ClipListing, EmptyState } from "../../library/ClipListing";
import { SortSelect, UploadedBySelect, ViewSwitch } from "../../library/controls";
import { parseClipSearch, type ClipSearch } from "../../library/search";
import { useClipView } from "../../library/view";

// The library's search, filter and sort (the top bar's search box searches the collection here), and whether
// sub-collections' clips are included, all in the URL.
type CollectionSearch = ClipSearch & { includeSubCollections?: true };

export const Route = createFileRoute("/_authenticated/collections/$id")({
  validateSearch: (search): CollectionSearch => ({
    ...parseClipSearch(search),
    includeSubCollections: search.includeSubCollections === true ? true : undefined,
  }),
  component: CollectionPage,
});

const authenticated = getRouteApi("/_authenticated");

type Collection = components["schemas"]["GetCollectionResponse"];

// The collection and its clips load at the same time: the clips need only the id in the URL. The sub-collections
// and clip counts come from the tree the sidebar already has.
function CollectionPage() {
  const { id } = Route.useParams();
  const search = Route.useSearch();
  const navigate = Route.useNavigate();
  const [view, setView] = useClipView();
  const items = useCollectionTree().data?.items ?? [];
  const collection = $api.useQuery(
    "get",
    "/collections/{id}",
    { params: { path: { id } } },
    { retry: (count, error) => error.status !== 404 && count < 3 },
  );

  if (collection.isError) {
    return collection.error.status === 404 ? (
      <EmptyState title="This collection doesn't exist">
        <span className="text-[15px] text-meta">It may have been deleted.</span>
      </EmptyState>
    ) : (
      <div
        role="alert"
        className="flex flex-wrap items-center gap-3 rounded-[10px] bg-danger-soft px-4 py-3 text-danger"
      >
        <span className="flex-1 text-[15px] font-semibold">Couldn't load the collection.</span>
        <button
          type="button"
          onClick={() => void collection.refetch()}
          className="cursor-pointer font-semibold underline"
        >
          Try again
        </button>
      </div>
    );
  }

  const children = childrenOf(items, id);
  const name = collection.data?.name ?? items.find((item) => item.id === id)?.name;

  return (
    <div className="flex flex-col gap-5">
      {collection.data ? (
        <Header collection={collection.data} items={items} subCollections={children} />
      ) : (
        <p className="text-[15px] text-meta">Loading collection…</p>
      )}

      {children.length > 0 && (
        <div className="flex flex-wrap items-center gap-2.5">
          <h2 className="font-mono text-xs font-normal tracking-[0.08em] text-meta uppercase">
            Inside
          </h2>
          {children.map((child) => (
            <Link
              key={child.id}
              to="/collections/$id"
              params={{ id: child.id }}
              className="flex h-10 max-w-full items-center gap-2 rounded-full border border-line bg-surface px-3.5 text-sm font-semibold text-text hover:bg-sunken"
            >
              <Folder size={16} aria-hidden="true" className="flex-none text-meta" />
              <span className="truncate">{child.name}</span>
              <span className="font-mono text-xs font-normal text-meta">{child.clipCount}</span>
            </Link>
          ))}
        </div>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line pt-3">
        {children.length > 0 ? (
          <label className="flex min-h-11 cursor-pointer items-center gap-3 text-[15px]">
            <input
              type="checkbox"
              role="switch"
              checked={search.includeSubCollections === true}
              onChange={(e) =>
                void navigate({
                  search: (prev) => ({
                    ...prev,
                    includeSubCollections: e.target.checked ? true : undefined,
                  }),
                })
              }
              className="size-4.5 cursor-pointer accent-accent"
            />
            Include clips in sub-collections
          </label>
        ) : (
          <span />
        )}
        <div className="flex flex-wrap items-center gap-2.5">
          <UploadedBySelect
            value={search.uploadedBy}
            onChange={(uploadedBy) =>
              void navigate({ search: (prev) => ({ ...prev, uploadedBy }) })
            }
          />
          <SortSelect
            sort={search.sort}
            order={search.order}
            onChange={(sort, order) =>
              void navigate({ search: (prev) => ({ ...prev, sort, order }) })
            }
          />
          <ViewSwitch view={view} onChange={setView} />
        </div>
      </div>

      <ClipListing
        // A listing of its own per collection: kept across collections, the previous one's clips would stay on
        // screen while the next one's load.
        key={id}
        query={{
          collectionId: id,
          includeSubCollections: search.includeSubCollections,
          q: search.q,
          uploadedBy: search.uploadedBy,
          sort: search.sort,
          order: search.order,
        }}
        view={view}
        empty={
          search.q !== undefined || search.uploadedBy !== undefined ? (
            <EmptyState
              title={search.q ? `No clips match “${search.q}”` : "No clips match this filter"}
            >
              <span className="text-[15px] text-meta">
                {search.includeSubCollections
                  ? "Searched this collection and its sub-collections."
                  : "Searched this collection, not its sub-collections."}
              </span>
              <div className="flex flex-wrap justify-center gap-2.5">
                <Link
                  from={Route.fullPath}
                  search={(prev) => ({ ...prev, q: undefined, uploadedBy: undefined })}
                  className={buttonClass}
                >
                  Show all clips in {name ?? "this collection"}
                </Link>
                {search.q && (
                  <Link to="/library" search={{ q: search.q }} className={buttonClass}>
                    Search the whole library
                  </Link>
                )}
              </div>
            </EmptyState>
          ) : search.includeSubCollections ? (
            <EmptyState title="No clips here or in its sub-collections yet" />
          ) : (
            <EmptyState title="No clips in this collection yet">
              {children.length > 0 && (
                <Link
                  from={Route.fullPath}
                  search={(prev) => ({ ...prev, includeSubCollections: true })}
                  className={buttonClass}
                >
                  Show clips in sub-collections
                </Link>
              )}
            </EmptyState>
          )
        }
      />
    </div>
  );
}

function Header({
  collection,
  items,
  subCollections,
}: {
  collection: Collection;
  items: CollectionItem[];
  subCollections: CollectionItem[];
}) {
  const search = Route.useSearch();
  const me = authenticated.useLoaderData();
  const ancestors = ancestorsOf(items, collection.id);
  const clipCount = items.find((item) => item.id === collection.id)?.clipCount;

  // The count is the collection's own clips. A total with its sub-collections can't be added up from the tree, since
  // a clip can be in several of them, so "of its own" says which count it is.
  const meta = [
    clipCount !== undefined &&
      `${plural(clipCount, "clip")}${subCollections.length > 0 ? " of its own" : ""}`,
    subCollections.length > 0 && plural(subCollections.length, "sub-collection"),
    `created by ${collection.createdBy.id === me.id ? "you" : collection.createdBy.displayName}`,
  ].filter(Boolean);

  return (
    <>
      <nav aria-label="Breadcrumb" className="flex flex-wrap items-center gap-2 text-sm text-meta">
        <Link to="/library" className="text-meta hover:underline">
          Library
        </Link>
        {ancestors.map((ancestor) => (
          <Fragment key={ancestor.id}>
            <span aria-hidden="true">/</span>
            <Link
              to="/collections/$id"
              params={{ id: ancestor.id }}
              className="text-meta hover:underline"
            >
              {ancestor.name}
            </Link>
          </Fragment>
        ))}
        <span aria-hidden="true">/</span>
        <span aria-current="page" className="font-semibold text-text">
          {collection.name}
        </span>
      </nav>

      <div className="flex flex-col gap-2">
        <h1 className="text-[34px] font-extrabold tracking-[-0.02em] break-words">
          {collection.name}
        </h1>
        {collection.description && (
          <p className="max-w-160 text-base leading-normal whitespace-pre-line text-meta">
            {collection.description}
          </p>
        )}
        <span className="font-mono text-xs text-meta">{meta.join(" · ")}</span>
        {search.q && <p className="text-[15px] text-meta">Clips matching “{search.q}”</p>}
      </div>
    </>
  );
}

function plural(count: number, noun: string) {
  return `${count} ${noun}${count === 1 ? "" : "s"}`;
}
