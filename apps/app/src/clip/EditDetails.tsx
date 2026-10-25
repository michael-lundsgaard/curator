import { useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { $api } from "../api/client";
import type { components } from "../api/schema";

export type ClipDetails = components["schemas"]["GetAssetResponse"];

const labelClass = "text-sm font-semibold";
const inputClass = "rounded-[10px] border border-line bg-bg px-3 text-[15px]";
const secondaryButtonClass =
  "flex h-11 cursor-pointer items-center rounded-[10px] border border-line bg-surface px-4.5 text-[15px] font-semibold hover:bg-sunken disabled:cursor-default disabled:opacity-60";

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
      queryClient.setQueryData(queryKey, (old) => old && { ...old, ...edited });
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
      <div className="flex flex-col gap-1.5">
        <label htmlFor="title" className={labelClass}>
          Title
        </label>
        <input
          id="title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          maxLength={255}
          className={`h-11 ${inputClass}`}
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <label htmlFor="description" className={labelClass}>
          Description
        </label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={4}
          maxLength={5000}
          className={`resize-y py-2.5 leading-normal ${inputClass}`}
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <label htmlFor="recorded" className={labelClass}>
          Recorded
        </label>
        <input
          id="recorded"
          type="datetime-local"
          value={recorded}
          onChange={(e) => setRecorded(e.target.value)}
          aria-describedby="recorded-hint"
          className={`h-11 ${inputClass}`}
        />
        <span id="recorded-hint" className="text-[13px] text-meta">
          Leave empty if you don't know. The library then sorts it by upload date.
        </span>
      </div>
      {edit.isError && (
        <p role="alert" className="rounded-[10px] bg-danger-soft px-3 py-2.5 text-sm text-danger">
          Couldn't save the changes. Try again.
        </p>
      )}
      <div className="flex gap-2.5">
        <button
          type="submit"
          disabled={edit.isPending}
          className="flex h-11 cursor-pointer items-center rounded-[10px] bg-accent px-4.5 text-[15px] font-semibold text-white hover:bg-accent-hover disabled:cursor-default disabled:opacity-60"
        >
          {edit.isPending ? "Saving…" : "Save"}
        </button>
        <button type="button" onClick={onDone} className={secondaryButtonClass}>
          Cancel
        </button>
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
