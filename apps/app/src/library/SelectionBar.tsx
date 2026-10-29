import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { useState, type ReactNode } from "react";
import { problemMessage } from "../api/problem";
import { AddToCollection } from "../collections/AddToCollection";
import { refreshCollections, removeClips } from "../collections/clips";
import { RemoveFromSubCollections } from "../collections/RemoveFromSubCollections";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { plural } from "./format";

const quietClass =
  "h-10 cursor-pointer rounded-lg px-3.5 text-sm font-semibold hover:bg-bg/15 disabled:cursor-default disabled:opacity-60";

// What can be done with the selected clips: add them to a collection and, on a collection's page, remove them from it
// (and from its sub-collections when their clips are shown). `onDone` gets what was done, and whether anything changed,
// to show once the selection is cleared. "Select all" shows until all are.
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
  onDone: (message: ReactNode, changed: boolean) => void;
  onSelectAll?: () => void;
  onClear: () => void;
}) {
  const queryClient = useQueryClient();
  const [adding, setAdding] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const one = assetIds.length === 1;
  const from = includeSubCollections
    ? `${collection?.name} and its sub-collections`
    : collection?.name;
  const remove = useMutation({
    mutationFn: (collectionId: string) =>
      removeClips(collectionId, assetIds, includeSubCollections),
    onSuccess: async (removedCount) => {
      // Waits for the listing, so the removed clips are gone when the selection is.
      await refreshCollections(queryClient);
      // Only when someone else removed them first.
      const skipped = assetIds.length - removedCount;
      onDone(
        removedCount === 0 ? (
          <>
            {one ? "The clip wasn't" : "The clips weren't"} in {from} anymore.
          </>
        ) : (
          <>
            Removed {plural(removedCount, "clip")} from {from}.
            {skipped > 0 && ` ${skipped === 1 ? "1 wasn't" : `${skipped} weren't`} in it anymore.`}
          </>
        ),
        removedCount > 0,
      );
    },
  });

  const removeError = remove.isError && (
    <FormError>{problemMessage(remove.error, "Couldn't remove the clips. Try again.")}</FormError>
  );

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
          // The collection's name is the page's heading, right above. With the clips of the collections inside it
          // shown, it asks first.
          <button
            type="button"
            onClick={() =>
              includeSubCollections ? setConfirming(true) : remove.mutate(collection.id)
            }
            disabled={remove.isPending}
            className={quietClass}
          >
            {remove.isPending ? "Removing…" : "Remove from collection"}
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
      {confirming && collection ? (
        // Removing clears the selection, which closes it; after an error it stays open to try again.
        <RemoveFromSubCollections
          collection={collection}
          clipCount={assetIds.length}
          pending={remove.isPending}
          error={removeError}
          onRemove={() => remove.mutate(collection.id)}
          onClose={() => setConfirming(false)}
        />
      ) : (
        removeError
      )}
      {adding && (
        <AddToCollection
          assetIds={assetIds}
          currentId={collection?.id}
          onAdded={(target, addedCount) => {
            const skipped = assetIds.length - addedCount;
            const name = (
              <Link to="/collections/$id" params={{ id: target.id }} className="underline">
                {target.name}
              </Link>
            );
            onDone(
              addedCount === 0 ? (
                <>
                  {one ? "The clip was" : "The clips were"} already in {name}.
                </>
              ) : (
                <>
                  Added {plural(addedCount, "clip")} to {name}.
                  {skipped > 0 && ` ${skipped === 1 ? "1 was" : `${skipped} were`} in it already.`}
                </>
              ),
              addedCount > 0,
            );
          }}
          onClose={() => setAdding(false)}
        />
      )}
    </div>
  );
}
