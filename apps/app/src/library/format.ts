// How the library shows metadata: "12 Jul 2026", "14:32", "2:27", "1920×1080", "96 MB".

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

export function formatTime(iso: string) {
  const date = new Date(iso);
  return `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

// Decimal units, as macOS and storage makers count them: 1 MB is 1,000,000 bytes.
const sizeUnits = ["KB", "MB", "GB", "TB"];

export function formatSize(bytes: number) {
  if (bytes < 1000) return `${bytes} bytes`;
  let size = bytes;
  let unit = -1;
  while (size >= 1000 && unit < sizeUnits.length - 1) {
    size /= 1000;
    unit++;
  }
  return `${size < 10 ? size.toFixed(1) : Math.round(size)} ${sizeUnits[unit]}`;
}
