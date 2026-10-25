import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "../api/client";
import { problemMessage } from "../api/problem";
import { Dialog } from "../ui/Dialog";
import { CollectionForm } from "./CollectionForm";
import type { CollectionItem } from "./tree";

// A collection at the top level, or inside `parent`. Opens the new collection's page once it's made.
export function NewCollection({
  parent,
  onClose,
}: {
  parent: Pick<CollectionItem, "id" | "name"> | null;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const create = $api.useMutation("post", "/collections", {
    onSuccess: async (created) => {
      // Waits for the tree, so the sidebar and the breadcrumb already have it when its page opens.
      await queryClient.invalidateQueries({ queryKey: ["get", "/collections"] });
      onClose();
      await navigate({ to: "/collections/$id", params: { id: created.id } });
    },
  });

  return (
    <Dialog
      title={parent ? `New collection in ${parent.name}` : "New collection"}
      onClose={onClose}
    >
      <CollectionForm
        saveLabel="Create collection"
        savingLabel="Creating…"
        saving={create.isPending}
        error={
          create.isError
            ? problemMessage(create.error, "Couldn't create the collection. Try again.")
            : undefined
        }
        onSave={(details) => create.mutate({ body: { ...details, parentId: parent?.id ?? null } })}
        onCancel={onClose}
      />
    </Dialog>
  );
}
