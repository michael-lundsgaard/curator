import { useQueryClient } from "@tanstack/react-query";
import { $api } from "../api/client";
import { problemMessage } from "../api/problem";
import type { components } from "../api/schema";
import { Dialog } from "../ui/Dialog";
import { CollectionForm } from "./CollectionForm";

export type Collection = components["schemas"]["GetCollectionResponse"];

// The name and description, which the API replaces together (PATCH /collections/{id}).
export function EditCollection({
  collection,
  onClose,
}: {
  collection: Collection;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const edit = $api.useMutation("patch", "/collections/{id}", {
    onSuccess: async (edited) => {
      const { queryKey } = $api.queryOptions("get", "/collections/{id}", {
        params: { path: { id: collection.id } },
      });
      queryClient.setQueryData<Collection>(queryKey, (old) => old && { ...old, ...edited });
      // The new name in the sidebar and the breadcrumbs of its sub-collections.
      await queryClient.invalidateQueries({ queryKey: ["get", "/collections"] });
      onClose();
    },
  });

  return (
    <Dialog title="Edit collection" onClose={onClose}>
      <CollectionForm
        initial={collection}
        saveLabel="Save"
        savingLabel="Saving…"
        saving={edit.isPending}
        error={
          edit.isError
            ? problemMessage(edit.error, "Couldn't save the changes. Try again.")
            : undefined
        }
        onSave={(details) =>
          edit.mutate({ params: { path: { id: collection.id } }, body: details })
        }
        onCancel={onClose}
      />
    </Dialog>
  );
}
