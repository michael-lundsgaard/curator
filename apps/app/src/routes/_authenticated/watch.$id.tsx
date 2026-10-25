import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute, getRouteApi, Link } from "@tanstack/react-router";
import { ChevronLeft } from "lucide-react";
import { useState, type ReactNode } from "react";
import { $api } from "../../api/client";
import { EditDetails, type ClipDetails } from "../../clip/EditDetails";
import { Player } from "../../clip/Player";
import {
  formatDate,
  formatDuration,
  formatResolution,
  formatSize,
  formatTime,
} from "../../library/format";
import { LoadError } from "../../ui/alerts";
import { Button } from "../../ui/Button";
import { EmptyState } from "../../ui/EmptyState";

export const Route = createFileRoute("/_authenticated/watch/$id")({
  component: Watch,
});

const authenticated = getRouteApi("/_authenticated");

function Watch() {
  const { id } = Route.useParams();
  const clip = $api.useQuery(
    "get",
    "/assets/{id}",
    { params: { path: { id } } },
    {
      // Fresh until shortly before the signed URLs expire, so coming back to the page or the tab fetches new ones
      // only when they're needed: new URLs reload the video.
      staleTime: (query) =>
        query.state.data
          ? Math.max(0, new Date(query.state.data.urlsExpireAt).getTime() - Date.now() - 60_000)
          : 0,
      retry: (count, error) => error.status !== 404 && count < 3,
    },
  );

  return (
    <div className="flex flex-col gap-5">
      <Link
        to="/library"
        className="flex h-10 items-center gap-1.5 self-start rounded-lg pr-3 pl-1.5 text-[15px] font-semibold text-text hover:bg-sunken"
      >
        <ChevronLeft size={18} aria-hidden="true" />
        Library
      </Link>
      {clip.isPending ? (
        <p className="text-[15px] text-meta">Loading clip…</p>
      ) : clip.isError ? (
        clip.error.status === 404 ? (
          <EmptyState title="This clip doesn't exist">
            <span className="text-[15px] text-meta">It may have been deleted.</span>
          </EmptyState>
        ) : (
          <LoadError onRetry={() => void clip.refetch()}>Couldn't load the clip.</LoadError>
        )
      ) : (
        <Clip clip={clip.data} onUrlsExpired={() => void clip.refetch()} />
      )}
    </div>
  );
}

function Clip({ clip, onUrlsExpired }: { clip: ClipDetails; onUrlsExpired: () => void }) {
  const me = authenticated.useLoaderData();
  const [editing, setEditing] = useState(false);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  // The API's rule (Asset.CanBeEditedBy): the uploader or an admin. Others don't see the buttons.
  const canChange = clip.uploadedBy.id === me.id || me.role === "Admin";

  return (
    // Most clips are 720p or 1440p: the details go beside the video only once the video still gets about 850px or
    // more, else below it, in two columns where they fit.
    <div className="grid items-start gap-8 2xl:grid-cols-[minmax(0,1fr)_360px]">
      <div className="min-w-0">
        {clip.playbackUrl ? (
          <Player src={clip.playbackUrl} expiresAt={clip.urlsExpireAt} onExpired={onUrlsExpired} />
        ) : (
          <div className="flex aspect-video items-center justify-center rounded-[14px] bg-sunken p-6 text-center text-[15px] text-meta">
            The upload hasn't finished, so there's nothing to play yet.
          </div>
        )}
      </div>

      <aside className="grid min-w-0 items-start gap-6 lg:grid-cols-2 2xl:grid-cols-1">
        {editing ? (
          <EditDetails clip={clip} onDone={() => setEditing(false)} />
        ) : (
          <div className="flex flex-col gap-6">
            <div className="flex flex-col gap-2.5">
              <h1 className="text-[30px] leading-[1.1] font-extrabold tracking-[-0.02em] break-words">
                {clip.title}
              </h1>
              {clip.description && (
                <p className="text-base leading-[1.55] whitespace-pre-line text-meta">
                  {clip.description}
                </p>
              )}
            </div>
            {canChange && (
              <div className="flex flex-wrap gap-2.5">
                <Button
                  onClick={() => {
                    setConfirmingDelete(false);
                    setEditing(true);
                  }}
                >
                  Edit details
                </Button>
                <Button variant="quiet-danger" onClick={() => setConfirmingDelete(true)}>
                  Delete
                </Button>
              </div>
            )}
            {confirmingDelete && (
              <ConfirmDelete clip={clip} onCancel={() => setConfirmingDelete(false)} />
            )}
          </div>
        )}

        <dl className="grid grid-cols-[max-content_minmax(0,1fr)] gap-x-5 gap-y-2.5 rounded-[14px] border border-line bg-surface p-5 text-sm">
          <Detail term="Recorded">
            {clip.recordedAt
              ? `${formatDate(clip.recordedAt)} · ${formatTime(clip.recordedAt)}`
              : "Unknown"}
          </Detail>
          <Detail term="Uploaded">
            {formatDate(clip.createdAt)} · by{" "}
            {clip.uploadedBy.id === me.id ? "you" : clip.uploadedBy.displayName}
          </Detail>
          {clip.durationMs !== null && (
            <Detail term="Length">{formatDuration(clip.durationMs)}</Detail>
          )}
          {formatResolution(clip.width, clip.height) && (
            <Detail term="Resolution">{formatResolution(clip.width, clip.height)}</Detail>
          )}
          <Detail term="File">
            {clip.originalFilename} · {formatSize(clip.sourceSizeBytes)}
          </Detail>
        </dl>
      </aside>
    </div>
  );
}

function ConfirmDelete({ clip, onCancel }: { clip: ClipDetails; onCancel: () => void }) {
  const queryClient = useQueryClient();
  const navigate = Route.useNavigate();
  const remove = $api.useMutation("delete", "/assets/{id}", {
    onSuccess: async () => {
      void queryClient.invalidateQueries({ queryKey: ["get", "/assets"] });
      await navigate({ to: "/library" });
      // Only once the page is gone: removed while it's open, the query would fetch the clip again and get a 404.
      queryClient.removeQueries({
        queryKey: $api.queryOptions("get", "/assets/{id}", { params: { path: { id: clip.id } } })
          .queryKey,
      });
    },
  });

  return (
    <div
      role="alertdialog"
      aria-labelledby="delete-title"
      aria-describedby="delete-description"
      className="flex flex-col gap-3 rounded-xl bg-danger-soft p-4"
    >
      <span id="delete-title" className="text-[15px] font-semibold break-words">
        Delete “{clip.title}”?
      </span>
      <span id="delete-description" className="text-sm leading-normal">
        It disappears from the library and every collection. This can't be undone in the app.
      </span>
      {remove.isError && (
        <span role="alert" className="text-sm font-semibold text-danger">
          Couldn't delete the clip. Try again.
        </span>
      )}
      <div className="flex gap-2.5">
        <Button
          variant="danger"
          onClick={() => remove.mutate({ params: { path: { id: clip.id } } })}
          disabled={remove.isPending}
        >
          {remove.isPending ? "Deleting…" : "Delete clip"}
        </Button>
        <Button onClick={onCancel}>Keep it</Button>
      </div>
    </div>
  );
}

function Detail({ term, children }: { term: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-meta">{term}</dt>
      <dd className="font-mono text-[13px] wrap-anywhere">{children}</dd>
    </>
  );
}
