import { Play } from "lucide-react";
import type { components } from "../api/schema";
import { formatDate, formatDuration } from "./format";

export type Clip = components["schemas"]["ListAssetsItem"];

// Cards with the clip's still, title, date and uploader. Until processing makes thumbnails, the still is a
// placeholder. Recorded falls back to uploaded, as the Recorded sort does.
export function ClipGrid({
  clips,
  uploaderName,
}: {
  clips: Clip[];
  uploaderName: (clip: Clip) => string;
}) {
  return (
    <ul className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-x-6 gap-y-7">
      {clips.map((clip) => (
        <li key={clip.id} className="flex flex-col gap-2.5">
          <div className="relative flex aspect-video items-center justify-center rounded-[10px] bg-sunken text-meta">
            <Play size={28} aria-hidden="true" />
            {clip.durationMs !== null && (
              <span className="absolute right-2 bottom-2 rounded bg-text/80 px-1.5 py-0.5 font-mono text-xs text-bg">
                {formatDuration(clip.durationMs)}
              </span>
            )}
          </div>
          <span className="text-[15px] leading-tight font-semibold break-words">{clip.title}</span>
          <span className="font-mono text-xs text-meta">
            {formatDate(clip.recordedAt ?? clip.createdAt)} · {uploaderName(clip)}
          </span>
        </li>
      ))}
    </ul>
  );
}
