import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "../api/client";
import { problemMessage } from "../api/problem";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { Dialog } from "../ui/Dialog";
import type { Collection } from "./EditCollection";
import { flatten, insideCount, type CollectionItem } from "./tree";

// Deleting a collection deletes its sub-collections at every level too (DELETE /collections/{id}), so the dialog
// names them. The clips stay. Afterwards the collection above it opens, or the library.
export function DeleteCollection({
  collection,
  items,
  onClose,
}: {
  collection: Collection;
  items: CollectionItem[];
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const inside = flatten(items, collection.id).map(({ item }) => item);
  const remove = $api.useMutation("delete", "/collections/{id}", {
    onSuccess: async () => {
      void queryClient.invalidateQueries({ queryKey: ["get", "/collections"] });
      await (collection.parentId
        ? navigate({ to: "/collections/$id", params: { id: collection.parentId } })
        : navigate({ to: "/library" }));
      // The parent's page is the same component as this one's, so the dialog would stay open on it.
      onClose();
      // Only once the page is gone: removed while it's open, the query would fetch the collection again and get a
      // 404. Its sub-collections' too, so Back doesn't show them from the cache.
      for (const id of [collection.id, ...inside.map((item) => item.id)]) {
        queryClient.removeQueries({
          queryKey: $api.queryOptions("get", "/collections/{id}", { params: { path: { id } } })
            .queryKey,
        });
      }
    },
  });

  return (
    <Dialog title={`Delete “${collection.name}”?`} onClose={onClose}>
      <div className="flex flex-col gap-2 text-[15px] leading-normal">
        {inside.length > 0 && (
          <p>
            This also deletes {insideCount(inside.length)}:{" "}
            {listOf(inside.map((item) => item.name))}.
          </p>
        )}
        <p className="text-meta">
          The clips stay in the library and in other collections. This can't be undone in the app.
        </p>
      </div>
      {remove.isError && (
        <FormError>
          {problemMessage(remove.error, "Couldn't delete the collection. Try again.")}
        </FormError>
      )}
      <div className="flex gap-2.5">
        <Button
          variant="danger"
          size="lg"
          onClick={() => remove.mutate({ params: { path: { id: collection.id } } })}
          disabled={remove.isPending}
        >
          {remove.isPending ? "Deleting…" : "Delete collection"}
        </Button>
        <Button size="lg" onClick={onClose} data-autofocus>
          Keep it
        </Button>
      </div>
    </Dialog>
  );
}

// "A", "A and B", "A, B and C", "A, B, C and 4 more": a long list would push the buttons off a phone's screen.
function listOf(names: string[]) {
  const shown = names.length > 4 ? names.slice(0, 3) : names;
  const rest = names.length - shown.length;
  const all = rest > 0 ? [...shown, `${rest} more`] : shown;
  return all.length === 1 ? all[0] : `${all.slice(0, -1).join(", ")} and ${all.at(-1)}`;
}
