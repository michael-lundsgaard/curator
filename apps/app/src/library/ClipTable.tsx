import { Link } from "@tanstack/react-router";
import type { Clip } from "./ClipGrid";
import { formatDate, formatDuration, formatResolution } from "./format";

const headerClass = "px-4 py-3 font-medium";
const metaCellClass = "px-4 py-2 font-mono text-[13px] whitespace-nowrap text-meta";

// The list view: one row per clip with every date and size. Scrolls sideways on a phone rather than squeezing the
// columns.
export function ClipTable({
  clips,
  uploaderName,
  selected,
  onToggle,
  onToggleAll,
}: {
  clips: Clip[];
  uploaderName: (clip: Clip) => string;
  selected: Set<string>;
  onToggle: (id: string) => void;
  onToggleAll: () => void;
}) {
  const selectedCount = clips.filter((clip) => selected.has(clip.id)).length;

  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-surface">
      <table className="w-full min-w-200 border-collapse text-sm">
        <thead>
          <tr className="text-left font-mono text-xs tracking-[0.06em] text-meta uppercase">
            <th scope="col" className="w-12 p-0">
              <label className="flex h-11 cursor-pointer items-center justify-end pr-1">
                <input
                  type="checkbox"
                  checked={selectedCount === clips.length}
                  // Half checked when some are: the browser has no attribute for it, only the property.
                  ref={(input) => {
                    if (input)
                      input.indeterminate = selectedCount > 0 && selectedCount < clips.length;
                  }}
                  onChange={onToggleAll}
                  aria-label="Select all clips shown"
                  className="size-4.5 cursor-pointer accent-accent"
                />
              </label>
            </th>
            <th scope="col" className={headerClass}>
              Title
            </th>
            <th scope="col" className={headerClass}>
              Recorded
            </th>
            <th scope="col" className={headerClass}>
              Uploaded
            </th>
            <th scope="col" className={headerClass}>
              By
            </th>
            <th scope="col" className={`${headerClass} text-right`}>
              Length
            </th>
            <th scope="col" className={`${headerClass} text-right`}>
              Resolution
            </th>
          </tr>
        </thead>
        <tbody>
          {clips.map((clip) => (
            <tr
              key={clip.id}
              className={`border-t border-line ${selected.has(clip.id) ? "bg-accent-soft" : "hover:bg-sunken"}`}
            >
              <td className="p-0">
                {/* The whole cell, so the checkbox is easy to hit. */}
                <label className="flex h-13 cursor-pointer items-center justify-end pr-1">
                  <input
                    type="checkbox"
                    checked={selected.has(clip.id)}
                    onChange={() => onToggle(clip.id)}
                    aria-label={`Select ${clip.title}`}
                    className="size-4.5 cursor-pointer accent-accent"
                  />
                </label>
              </td>
              <td className="px-4 py-2 font-semibold">
                <div className="flex items-center gap-3">
                  <span className="h-9 w-16 flex-none rounded-md bg-sunken" aria-hidden="true" />
                  <Link
                    to="/watch/$id"
                    params={{ id: clip.id }}
                    className="text-text hover:underline"
                  >
                    {clip.title}
                  </Link>
                </div>
              </td>
              <td className={metaCellClass}>
                {clip.recordedAt ? formatDate(clip.recordedAt) : "—"}
              </td>
              <td className={metaCellClass}>{formatDate(clip.createdAt)}</td>
              <td className="px-4 py-2">{uploaderName(clip)}</td>
              <td className={`${metaCellClass} text-right`}>
                {clip.durationMs !== null ? formatDuration(clip.durationMs) : "—"}
              </td>
              <td className={`${metaCellClass} text-right`}>
                {formatResolution(clip.width, clip.height) ?? "—"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
