import { useState, type FormEvent } from "react";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { TextArea, TextField } from "../ui/fields";

export type CollectionDetails = { name: string; description: string | null };

// The name and description of a new collection or one being edited, in a dialog. An empty description is sent as
// null, which the API stores as none.
export function CollectionForm({
  initial = { name: "", description: null },
  saveLabel,
  savingLabel,
  saving,
  error,
  onSave,
  onCancel,
}: {
  initial?: CollectionDetails;
  saveLabel: string;
  savingLabel: string;
  saving: boolean;
  error?: string;
  onSave: (details: CollectionDetails) => void;
  onCancel: () => void;
}) {
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description ?? "");

  const save = (e: FormEvent) => {
    e.preventDefault();
    onSave({ name: name.trim(), description: description.trim() || null });
  };

  return (
    <form onSubmit={save} className="flex flex-col gap-4">
      <TextField
        id="collection-name"
        label="Name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
        maxLength={255}
        placeholder="A game, a trip, an occasion"
      />
      <TextArea
        id="collection-description"
        label="Description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        rows={3}
        maxLength={5000}
        hint="Optional. Shown under the name on the collection's page."
      />
      {error && <FormError>{error}</FormError>}
      <div className="flex gap-2.5">
        <Button type="submit" variant="primary" size="lg" disabled={saving}>
          {saving ? savingLabel : saveLabel}
        </Button>
        <Button size="lg" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
