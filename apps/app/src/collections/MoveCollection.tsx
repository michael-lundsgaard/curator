import { useQueryClient } from "@tanstack/react-query";
import { Folder, Layers } from "lucide-react";
import { useState, type FormEvent } from "react";
import { $api } from "../api/client";
import { problemMessage } from "../api/problem";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { Dialog } from "../ui/Dialog";
import type { Collection } from "./EditCollection";
import { flatten, heightOf, maxDepth, type CollectionItem } from "./tree";
import { TreeOption, TreeOptions } from "./TreeOption";

// Where to move a collection: the top level or another collection, picked from the tree. Its own sub-collections
// aren't listed, and collections too deep for it with its sub-collections are greyed out, the API's rules, so a move
// fails only when someone changed the tree in the meantime.
export function MoveCollection({
  collection,
  items,
  onClose,
}: {
  collection: Collection;
  items: CollectionItem[];
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [parentId, setParentId] = useState(collection.parentId);
  const move = $api.useMutation("post", "/collections/{id}/move", {
    onSuccess: async (moved) => {
      const { queryKey } = $api.queryOptions("get", "/collections/{id}", {
        params: { path: { id: collection.id } },
      });
      queryClient.setQueryData<Collection>(queryKey, (old) => old && { ...old, ...moved });
      // The sidebar and the breadcrumb, which come from the tree.
      await queryClient.invalidateQueries({ queryKey: ["get", "/collections"] });
      onClose();
    },
  });

  const height = heightOf(items, collection.id);
  const inside = new Set(flatten(items, collection.id).map(({ item }) => item.id));
  // The API's depth starts at 1, flatten's at 0: a collection at depth d holds levels d + 2 and further down.
  const targets = flatten(items)
    .filter(({ item }) => item.id !== collection.id && !inside.has(item.id))
    .map(({ item, depth }) => ({ item, depth, fits: depth + 1 + height <= maxDepth }));

  const save = (e: FormEvent) => {
    e.preventDefault();
    move.mutate({ params: { path: { id: collection.id } }, body: { parentId } });
  };

  return (
    <Dialog title={`Move “${collection.name}”`} onClose={onClose}>
      <form onSubmit={save} className="flex flex-col gap-4">
        <fieldset className="flex flex-col gap-2">
          <legend className="pb-2 text-sm font-semibold">Move into</legend>
          {/* The collections indented under "Top level". */}
          <TreeOptions>
            <TreeOption
              checked={parentId === null}
              onChange={() => setParentId(null)}
              depth={0}
              icon={<Layers size={16} />}
            >
              Top level
            </TreeOption>
            {targets.map(({ item, depth, fits }) => (
              <TreeOption
                key={item.id}
                checked={parentId === item.id}
                onChange={() => setParentId(item.id)}
                depth={depth + 1}
                disabled={!fits}
                icon={<Folder size={16} />}
              >
                {item.name}
              </TreeOption>
            ))}
          </TreeOptions>
          {targets.some(({ fits }) => !fits) && (
            <span className="text-[13px] text-meta">
              Collections go {maxDepth} levels deep, so the greyed out ones have no room for it
              {height > 1 ? " and its sub-collections" : ""}.
            </span>
          )}
        </fieldset>
        {move.isError && (
          <FormError>
            {problemMessage(move.error, "Couldn't move the collection. Try again.")}
          </FormError>
        )}
        <div className="flex gap-2.5">
          <Button
            type="submit"
            variant="primary"
            size="lg"
            disabled={move.isPending || parentId === collection.parentId}
          >
            {move.isPending ? "Moving…" : "Move"}
          </Button>
          <Button size="lg" onClick={onClose}>
            Cancel
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
