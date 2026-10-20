// The brand mark (docs/design.md): the frame takes the text color, the square stays cobalt in both themes.
export function Logo() {
  return (
    <svg width="26" height="26" viewBox="0 0 64 64" aria-hidden="true">
      <path d="M54 12H12V52H54" fill="none" stroke="currentColor" strokeWidth="8" />
      <rect x="28" y="25" width="14" height="14" fill="#2F4BFF" />
    </svg>
  );
}
