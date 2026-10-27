import { Link } from "@tanstack/react-router";
import { Check, Play } from "lucide-react";
import type { MouseEvent } from "react";
import type { components } from "../api/schema";
import { formatDate, formatDuration } from "./format";

export type Clip = components["schemas"]["ListAssetsItem"];

// Cards with the clip's still, title, date and uploader. Until processing makes thumbnails, the still is a
// placeholder. Recorded falls back to uploaded, as the Recorded sort does. A card links to the clip; a round check on
// the still's corner selects it, shown on hover (always on a touch screen, which has none). Once a clip is selected, a
// click on a card selects it too, as in a photo app, so a click that misses the check doesn't open the clip.
// Ctrl- or Cmd-click still opens it in a new tab.
export function ClipGrid({
  clips,
  uploaderName,
  selected,
  onToggle,
}: {
  clips: Clip[];
  uploaderName: (clip: Clip) => string;
  selected: Set<string>;
  onToggle: (id: string) => void;
}) {
  const selecting = selected.size > 0;

  const select = (e: MouseEvent, id: string) => {
    if (!selecting || e.ctrlKey || e.metaKey || e.shiftKey) return;
    e.preventDefault();
    onToggle(id);
  };

  return (
    // As many columns as fit. Wider cards on a big screen, so the stills don't shrink to stamps in ten columns.
    <ul className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-x-6 gap-y-7 2xl:grid-cols-[repeat(auto-fill,minmax(280px,1fr))]">
      {clips.map((clip) => (
        <li key={clip.id} className="group/card relative">
          <Link
            to="/watch/$id"
            params={{ id: clip.id }}
            onClick={(e) => select(e, clip.id)}
            className="group flex flex-col gap-2.5 rounded-[10px] text-text"
          >
            <div
              className={`relative flex aspect-video items-center justify-center rounded-[10px] bg-sunken text-meta group-hover:text-text ${selected.has(clip.id) ? "ring-3 ring-accent" : ""}`}
            >
              <Play size={28} aria-hidden="true" />
              {clip.durationMs !== null && (
                <span className="absolute right-2 bottom-2 rounded bg-text/80 px-1.5 py-0.5 font-mono text-xs text-bg">
                  {formatDuration(clip.durationMs)}
                </span>
              )}
            </div>
            <span
              className={`text-[15px] leading-tight font-semibold break-words ${selecting ? "" : "group-hover:underline"}`}
            >
              {clip.title}
            </span>
            <span className="font-mono text-xs text-meta">
              {formatDate(clip.recordedAt ?? clip.createdAt)} · {uploaderName(clip)}
            </span>
          </Link>
          {/* Beside the link, not in it: a checkbox inside a link would open the clip too. The label is bigger than the
              check, so it's easy to hit. White over a tint in both themes, as it sits on the still, not on the page. */}
          <label
            className={`absolute top-1 left-1 flex size-11 cursor-pointer items-center justify-center has-focus-visible:opacity-100 pointer-coarse:opacity-100 ${selecting ? "" : "opacity-0 group-hover/card:opacity-100"}`}
          >
            <input
              type="checkbox"
              checked={selected.has(clip.id)}
              onChange={() => onToggle(clip.id)}
              aria-label={`Select ${clip.title}`}
              className="peer size-6 cursor-pointer appearance-none rounded-full border-2 border-white bg-black/25 shadow-sm checked:border-accent checked:bg-accent focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            />
            <Check
              size={14}
              strokeWidth={3}
              aria-hidden="true"
              className="pointer-events-none absolute hidden text-white peer-checked:block"
            />
          </label>
        </li>
      ))}
    </ul>
  );
}
