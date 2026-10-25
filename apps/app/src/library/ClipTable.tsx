import type { Clip } from "./ClipGrid";
import { formatDate, formatDuration, formatResolution } from "./format";

const headerClass = "px-4 py-3 font-medium";
const metaCellClass = "px-4 py-2 font-mono text-[13px] whitespace-nowrap text-meta";

// The list view: one row per clip with every date and size. Scrolls sideways on a phone rather than squeezing the
// columns.
export function ClipTable({
  clips,
  uploaderName,
}: {
  clips: Clip[];
  uploaderName: (clip: Clip) => string;
}) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-surface">
      <table className="w-full min-w-190 border-collapse text-sm">
        <thead>
          <tr className="text-left font-mono text-xs tracking-[0.06em] text-meta uppercase">
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
            <tr key={clip.id} className="border-t border-line hover:bg-sunken">
              <td className="px-4 py-2 font-semibold">
                <div className="flex items-center gap-3">
                  <span className="h-9 w-16 flex-none rounded-md bg-sunken" aria-hidden="true" />
                  {clip.title}
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
