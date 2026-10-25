export type ButtonStyle = {
  // Primary is the screen's one main action (design.md: one accent per screen); quiet-danger is a Delete that only
  // asks, before a confirmation with a danger button.
  variant?: "primary" | "secondary" | "danger" | "quiet-danger";
  // 40px for actions on a page, 44px in forms and below listings, 48px on the sign-in pages.
  size?: "md" | "lg" | "xl";
};

const variants = {
  primary: "bg-accent text-white hover:bg-accent-hover",
  secondary: "border border-line bg-surface text-text hover:bg-sunken",
  danger: "bg-danger text-bg",
  "quiet-danger": "text-danger hover:bg-danger-soft",
};

const sizes = {
  md: "h-10 rounded-lg px-3.5 text-sm",
  lg: "h-11 rounded-[10px] px-5 text-[15px]",
  xl: "h-12 rounded-[10px] px-5 text-base",
};

// For a Link that looks like a button; a button uses <Button>. In a file of its own, since Vite's fast refresh
// needs a file of components to export only components.
export function buttonClass({ variant = "secondary", size = "md" }: ButtonStyle = {}) {
  return `flex cursor-pointer items-center justify-center gap-2 font-semibold disabled:cursor-default disabled:opacity-60 ${variants[variant]} ${sizes[size]}`;
}
