import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Folder } from "lucide-react";
import { useState, type FormEvent } from "react";
import { problemMessage } from "../api/problem";
import { plural } from "../library/format";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { Dialog } from "../ui/Dialog";
import { addClips, refreshCollections } from "./clips";
import { flatten, useCollectionTree, type CollectionItem } from "./tree";
import { TreeOption, TreeOptions } from "./TreeOption";

// Which collection the selected clips go into, picked from the tree. On a collection's page that collection is greyed
// out: its clips are in it already. Reports how many were added, which leaves out those that were in it before.
export function AddToCollection({
  assetIds,
  currentId,
  onAdded,
  onClose,
}: {
  assetIds: string[];
  currentId?: string;
  onAdded: (collection: CollectionItem, addedCount: number) => void;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const tree = useCollectionTree();
  const [target, setTarget] = useState<CollectionItem | null>(null);
  const add = useMutation({
    mutationFn: (collection: CollectionItem) => addClips(collection.id, assetIds),
    onSuccess: (addedCount, collection) => {
      void refreshCollections(queryClient);
      onAdded(collection, addedCount);
    },
  });

  const save = (e: FormEvent) => {
    e.preventDefault();
    if (target) add.mutate(target);
  };

  const collections = tree.data ? flatten(tree.data.items) : [];

  return (
    <Dialog title={`Add ${plural(assetIds.length, "clip")} to a collection`} onClose={onClose}>
      <form onSubmit={save} className="flex flex-col gap-4">
        {tree.isPending ? (
          <p className="text-[15px] text-meta">Loading collections…</p>
        ) : collections.length === 0 ? (
          <p className="text-[15px] text-meta">
            There are no collections yet. Make one with the “+” next to Collections in the sidebar.
          </p>
        ) : (
          <fieldset className="flex flex-col gap-2">
            <legend className="pb-2 text-sm font-semibold">Add to</legend>
            <TreeOptions>
              {collections.map(({ item, depth }) => (
                <TreeOption
                  key={item.id}
                  checked={target?.id === item.id}
                  onChange={() => setTarget(item)}
                  depth={depth}
                  disabled={item.id === currentId}
                  icon={<Folder size={16} />}
                >
                  {item.name}
                </TreeOption>
              ))}
            </TreeOptions>
          </fieldset>
        )}
        {add.isError && (
          <FormError>{problemMessage(add.error, "Couldn't add the clips. Try again.")}</FormError>
        )}
        <div className="flex gap-2.5">
          <Button type="submit" variant="primary" size="lg" disabled={add.isPending || !target}>
            {add.isPending ? "Adding…" : "Add to collection"}
          </Button>
          <Button size="lg" onClick={onClose}>
            Cancel
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
