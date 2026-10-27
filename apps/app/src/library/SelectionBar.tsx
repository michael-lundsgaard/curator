import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { useState, type ReactNode } from "react";
import { problemMessage } from "../api/problem";
import { AddToCollection } from "../collections/AddToCollection";
import { refreshCollections, removeClips } from "../collections/clips";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { plural } from "./format";

const quietClass =
  "h-10 cursor-pointer rounded-lg px-3.5 text-sm font-semibold hover:bg-bg/15 disabled:cursor-default disabled:opacity-60";

// What can be done with the selected clips: add them to a collection and, on a collection's page, remove them from it.
// `onDone` gets what was done, to show once the selection is cleared. "Select all" shows until all are.
export function SelectionBar({
  assetIds,
  collection,
  includeSubCollections,
  onDone,
  onSelectAll,
  onClear,
}: {
  assetIds: string[];
  collection?: { id: string; name: string };
  includeSubCollections?: boolean;
  onDone: (message: ReactNode) => void;
  onSelectAll?: () => void;
  onClear: () => void;
}) {
  const queryClient = useQueryClient();
  const [adding, setAdding] = useState(false);
  const remove = useMutation({
    mutationFn: (collectionId: string) => removeClips(collectionId, assetIds),
    onSuccess: async (removedCount) => {
      // Waits for the listing, so the removed clips are gone when the selection is.
      await refreshCollections(queryClient);
      const skipped = assetIds.length - removedCount;
      // With sub-collections' clips shown, a selected clip may be only in one of them, which removing skips.
      const why = includeSubCollections
        ? `${skipped === 1 ? "1 is" : `${skipped} are`} only in its sub-collections.`
        : `${skipped === 1 ? "1 wasn't" : `${skipped} weren't`} in it anymore.`;
      onDone(
        <>
          Removed {plural(removedCount, "clip")} from {collection?.name}.{skipped > 0 && ` ${why}`}
        </>,
      );
    },
  });

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2.5 rounded-xl bg-text py-2.5 pr-3 pl-4 text-bg shadow-lg">
        <span className="flex-auto text-[15px] font-semibold">
          {plural(assetIds.length, "clip")} selected
        </span>
        <Button variant="primary" onClick={() => setAdding(true)} disabled={remove.isPending}>
          {collection ? "Add to another collection" : "Add to collection"}
        </Button>
        {collection && (
          <button
            type="button"
            onClick={() => remove.mutate(collection.id)}
            disabled={remove.isPending}
            className={quietClass}
          >
            {remove.isPending ? "Removing…" : `Remove from ${collection.name}`}
          </button>
        )}
        {onSelectAll && (
          <button
            type="button"
            onClick={onSelectAll}
            disabled={remove.isPending}
            className={quietClass}
          >
            Select all
          </button>
        )}
        <button type="button" onClick={onClear} disabled={remove.isPending} className={quietClass}>
          Clear
        </button>
      </div>
      {remove.isError && (
        <FormError>
          {problemMessage(remove.error, "Couldn't remove the clips. Try again.")}
        </FormError>
      )}
      {adding && (
        <AddToCollection
          assetIds={assetIds}
          currentId={collection?.id}
          onAdded={(target, addedCount) => {
            const skipped = assetIds.length - addedCount;
            onDone(
              <>
                Added {plural(addedCount, "clip")} to{" "}
                <Link
                  to="/collections/$id"
                  params={{ id: target.id }}
                  className="text-ok underline"
                >
                  {target.name}
                </Link>
                .{skipped > 0 && ` ${skipped === 1 ? "1 was" : `${skipped} were`} in it already.`}
              </>,
            );
          }}
          onClose={() => setAdding(false)}
        />
      )}
    </div>
  );
}
