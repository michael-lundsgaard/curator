import { useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { $api } from "../api/client";
import type { components } from "../api/schema";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { TextArea, TextField } from "../ui/fields";

export type ClipDetails = components["schemas"]["GetAssetResponse"];

// The title, description and recording time, which the API replaces together (PATCH /assets/{id}).
export function EditDetails({ clip, onDone }: { clip: ClipDetails; onDone: () => void }) {
  const queryClient = useQueryClient();
  const recordedInitially = toLocalInput(clip.recordedAt);
  const [title, setTitle] = useState(clip.title);
  const [description, setDescription] = useState(clip.description ?? "");
  const [recorded, setRecorded] = useState(recordedInitially);

  const edit = $api.useMutation("patch", "/assets/{id}", {
    onSuccess: (edited) => {
      // Merged into the clip rather than fetched again: a fetch signs new URLs, and a new URL restarts the video.
      const { queryKey } = $api.queryOptions("get", "/assets/{id}", {
        params: { path: { id: clip.id } },
      });
      queryClient.setQueryData<ClipDetails>(queryKey, (old) => old && { ...old, ...edited });
      void queryClient.invalidateQueries({ queryKey: ["get", "/assets"] });
      onDone();
    },
  });

  const save = (e: FormEvent) => {
    e.preventDefault();
    edit.mutate({
      params: { path: { id: clip.id } },
      body: {
        title: title.trim(),
        description: description.trim() || null,
        // The input shows minutes, so an untouched time is sent as it was, seconds and all.
        recordedAt:
          recorded === recordedInitially
            ? clip.recordedAt
            : recorded
              ? new Date(recorded).toISOString()
              : null,
      },
    });
  };

  return (
    <form
      onSubmit={save}
      className="flex flex-col gap-4 rounded-[14px] border border-line bg-surface p-5"
    >
      <h2 className="text-lg font-semibold">Edit details</h2>
      <TextField
        id="title"
        label="Title"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        required
        maxLength={255}
      />
      <TextArea
        id="description"
        label="Description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        rows={4}
        maxLength={5000}
      />
      <TextField
        id="recorded"
        label="Recorded"
        type="datetime-local"
        value={recorded}
        onChange={(e) => setRecorded(e.target.value)}
        hint="Leave empty if you don't know. The library then sorts it by upload date."
      />
      {edit.isError && <FormError>Couldn't save the changes. Try again.</FormError>}
      <div className="flex gap-2.5">
        <Button type="submit" variant="primary" size="lg" disabled={edit.isPending}>
          {edit.isPending ? "Saving…" : "Save"}
        </Button>
        <Button size="lg" onClick={onDone}>
          Cancel
        </Button>
      </div>
    </form>
  );
}

// <input type="datetime-local"> takes the browser's local time without a zone: "2026-07-12T14:32".
function toLocalInput(iso: string | null) {
  if (!iso) return "";
  const date = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
