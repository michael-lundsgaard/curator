# Design

The app's look: the **Curator** brand (chosen 2026-10-19 over the _Magpie_ exploration), with a light and a dark
theme. Clickable mocks of the main screens (sign in, reset password, home, library, collection, clip details,
upload, admin): [Curator app mocks](https://claude.ai/artifact/Kpt91Mp2x6zgJLhhuogFSr). They show what each
screen does, not final copy or exact spacing.

## Brand

- Tagline: _Every clip, in its place._ Secondary line: _Keep. Find. Show._
- Logo mark: an open frame with a cobalt square, next to the word "Curator" in Schibsted Grotesk 800 with
  `letter-spacing: -0.03em`. The frame takes the text color, so it works in both themes:

  ```html
  <svg viewBox="0 0 64 64" aria-hidden="true">
    <path d="M54 12H12V52H54" fill="none" stroke="currentColor" stroke-width="8" />
    <rect x="28" y="25" width="14" height="14" fill="#2F4BFF" />
  </svg>
  ```

- Type: **Schibsted Grotesk** (400, 600, 800) for headings and interface, **IBM Plex Mono** (400, 500) for
  metadata: dates, lengths, sizes, counts, status badges. Both from Google Fonts.
- One accent per screen: cobalt for the main action.

## Colors

Defined once as CSS variables per theme. The light theme is the brand board's palette (Gallery, Frame, Slate,
Ink, Cobalt); the dark theme is derived from it. Text colors pass 4.5:1 on their backgrounds.

| Token           | Light     | Dark      | Use                                       |
| --------------- | --------- | --------- | ----------------------------------------- |
| `--bg`          | `#F3F4F1` | `#0E1116` | Page background                           |
| `--surface`     | `#FFFFFF` | `#171A21` | Cards, sidebar, inputs                    |
| `--sunken`      | `#E9EAE6` | `#1E222B` | Hover, progress tracks, segmented control |
| `--line`        | `#DCDFE3` | `#2A2F39` | Borders and dividers                      |
| `--text`        | `#0E1116` | `#F3F4F1` | Text                                      |
| `--meta`        | `#5C6068` | `#9AA0A8` | Secondary text and metadata               |
| `--accent`      | `#2F4BFF` | `#3D57FF` | Primary buttons (white text)              |
| `--accent-soft` | `#E4E8FF` | `#1C2453` | Selected nav item, chips                  |
| `--link`        | `#2F4BFF` | `#8C9BFF` | Links and text on `--accent-soft`         |
| `--warn`        | `#8A5200` | `#F2B35B` | `Pending` (on `--warn-soft`)              |
| `--warn-soft`   | `#FBEBD3` | `#3A2B12` |                                           |
| `--danger`      | `#B42318` | `#FF8A80` | `Failed`, delete (on `--danger-soft`)     |
| `--danger-soft` | `#FDE7E5` | `#3D1715` |                                           |
| `--ok`          | `#1D7A4A` | `#5CC98E` | Done, success messages (on `--ok-soft`)   |
| `--ok-soft`     | `#DDF3E6` | `#14301F` |                                           |

## Light and dark

The theme follows the operating system (`prefers-color-scheme`) until the user picks one with the switch in the
top bar: System, Light or Dark. The choice is remembered in the browser (`localStorage`), since it's a preference
for that device, not part of the profile.
