import type { ReactNode } from "react";
import { plural } from "../library/format";
import { Button } from "../ui/Button";
import { Dialog } from "../ui/Dialog";

// Asks before removing clips from a collection and its sub-collections, which the collection's page does while it
// shows their clips. Unlike removing from the collection alone, it's hard to undo: nothing shows afterwards which
// sub-collection each clip was in. It doesn't name them: the page doesn't know which ones the selected clips are in.
// The selection bar does the removing; `error` is why it failed.
export function RemoveFromSubCollections({
  collection,
  clipCount,
  pending,
  error,
  onRemove,
  onClose,
}: {
  collection: { id: string; name: string };
  clipCount: number;
  pending: boolean;
  error: ReactNode;
  onRemove: () => void;
  onClose: () => void;
}) {
  const one = clipCount === 1;

  return (
    <Dialog
      title={`Remove ${plural(clipCount, "clip")} from “${collection.name}” and its sub-collections?`}
      onClose={onClose}
    >
      <p className="text-[15px] leading-normal text-meta">
        {one
          ? "It's taken out of every sub-collection it's in. The clip stays in the library and in other collections."
          : "They're taken out of every sub-collection they're in. The clips stay in the library and in other collections."}
      </p>
      {error}
      <div className="flex gap-2.5">
        <Button variant="primary" size="lg" onClick={onRemove} disabled={pending}>
          {pending ? "Removing…" : one ? "Remove clip" : "Remove clips"}
        </Button>
        <Button size="lg" onClick={onClose} data-autofocus>
          {one ? "Keep it" : "Keep them"}
        </Button>
      </div>
    </Dialog>
  );
}
