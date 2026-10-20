# 0010: Tailwind CSS for styling

- **Status:** Accepted
- **Date:** 2026-10-20

## Context

The app has to look like [design.md](../design.md) and its mocks: a fixed set of color tokens with a light and a
dark value each, two typefaces, and screens built from the same few pieces (nav items, buttons, cards, badges).
The theme follows the operating system until the user picks one in the top bar, so every color has to switch at
runtime, not at build time.

## Decision

**Tailwind CSS v4 through `@tailwindcss/vite`, with the design tokens as theme variables.**

- `src/index.css` declares the tokens in `@theme` as `--color-bg`, `--color-surface`, … (the names from
  design.md), which makes utilities like `bg-surface`, `text-meta` and `border-line`. Each utility reads its
  variable (`background-color: var(--color-surface)`), so `[data-theme="dark"]` overriding the variables
  switches every utility at once. No `dark:` variants: a component names a token, not a theme.
- The default palette is cleared (`--color-*: initial`), so only the design's colors exist and `bg-blue-500`
  doesn't compile.
- `--font-sans` and `--font-mono` are Schibsted Grotesk and IBM Plex Mono, loaded from Google Fonts in
  `index.html`.
- `data-theme` is set on `<html>` by a small inline script in `index.html` before the first paint, and kept in
  step by `src/theme.ts` (`useTheme`).
- Sizes the mocks use that aren't on Tailwind's scale are written as arbitrary values (`text-[15px]`). When the
  same one keeps coming back, it becomes a theme variable.

## Alternatives considered

- **Plain CSS with CSS Modules.** The tokens as CSS variables in a global file and a `.module.css` per
  component. No dependency and nothing between the mocks' styles and the app's. Not chosen: Tailwind is what's
  used at work, and agents write it well, which matters in a project built largely by an agent.
- **`dark:` variants instead of overriding variables.** Every colored element would name both colors
  (`bg-white dark:bg-[#171A21]`), so the tokens would be repeated across components instead of defined once.

## Consequences

- Markup carries the styling, so class lists get long. Repeated patterns become components (`NavItem`), not
  `@apply` classes.
- A token is added in three places: design.md's table, `@theme`, and the `[data-theme="dark"]` block.
- The Tailwind VS Code extension (already recommended) completes and previews the token utilities.
