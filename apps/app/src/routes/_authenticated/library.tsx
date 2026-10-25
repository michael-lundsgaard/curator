import { createFileRoute, Link } from "@tanstack/react-router";
import { buttonClass, ClipListing, EmptyState } from "../../library/ClipListing";
import { SortSelect, UploadedBySelect, ViewSwitch } from "../../library/controls";
import { parseClipSearch } from "../../library/search";
import { useClipView } from "../../library/view";

// How the clips are shown (grid or list) is a preference, see view.ts; what's shown is in the URL, see search.ts.
export const Route = createFileRoute("/_authenticated/library")({
  validateSearch: parseClipSearch,
  component: Library,
});

function Library() {
  const search = Route.useSearch();
  const navigate = Route.useNavigate();
  const [view, setView] = useClipView();

  const filtered = search.q !== undefined || search.uploadedBy !== undefined;

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-1.5">
          <h1 className="text-[34px] font-extrabold tracking-[-0.02em]">Library</h1>
          {search.q && <p className="text-[15px] text-meta">Clips matching “{search.q}”</p>}
        </div>
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
        query={{
          q: search.q,
          sort: search.sort,
          order: search.order,
          uploadedBy: search.uploadedBy,
        }}
        view={view}
        empty={
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
        }
      />
    </div>
  );
}
