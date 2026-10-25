// How the library shows metadata: "12 Jul 2026", "2:27", "1920×1080".

// Not Intl's en-GB format, which writes September as "Sept": every month is three letters here.
const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

// In the browser's time zone, so a clip recorded late in the evening keeps that evening's date.
export function formatDate(iso: string) {
  const date = new Date(iso);
  return `${date.getDate()} ${months[date.getMonth()]} ${date.getFullYear()}`;
}

export function formatDuration(ms: number) {
  const seconds = Math.round(ms / 1000);
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = String(seconds % 60).padStart(2, "0");
  return h > 0 ? `${h}:${String(m).padStart(2, "0")}:${s}` : `${m}:${s}`;
}

export function formatResolution(width: number | null, height: number | null) {
  return width && height ? `${width}×${height}` : null;
}
