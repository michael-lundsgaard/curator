---
name: verify
description: Build, lint, format-check and test what changed in the Curator API and app before calling a step done or committing. Use when the user asks to verify, check or "make sure it works", or before proposing a commit.
---

## What to run

List the changed files with `git status --porcelain` (staged, unstaged and untracked) and pick the parts:

| Changed                                             | Run              |
| --------------------------------------------------- | ---------------- |
| `apps/api/**`                                       | API              |
| `apps/api/openapi.json`                             | API and App      |
| `apps/app/**`                                       | App              |
| `.editorconfig`                                     | API (format)     |
| Anything else (docs, `.claude`, `.vscode`, `infra`) | Formatting alone |

Formatting always runs. Run everything when the working tree is clean (verifying what's already committed), when
the user asks for all (`/verify all`), or when a change could affect a part the table doesn't send it to.

Run the chosen parts in the order below, stopping at the first failure. Start the report with the parts chosen and
why, e.g. "App and Formatting (only apps/app changed)".

## API

From `apps/api`:

1. `dotnet build Curator.slnx -v q`
2. `dotnet format Curator.slnx --verify-no-changes`. If it fails, run `dotnet format Curator.slnx` and show the diff.
3. `dotnet test Curator.slnx --no-build`

If the build fails because `bin/` is locked by a running `Curator.Api` process, don't kill it. Re-run steps 1 and 3
with `--artifacts-path` pointing at a temp/scratchpad directory and drop `--no-build`.

## App

From `apps/app` (run `npm ci` first if `node_modules` is missing):

4. `npm run check:api`. It fails when `src/api/schema.d.ts` doesn't match `openapi.json`, and writes nothing. On a
   failure, run `npm run generate:api` and show the diff: it belongs in the commit with the `openapi.json` change.
5. `npm run lint`
6. `npm run build` (typechecks, then bundles)

## Formatting

From the repo root:

7. `npx prettier --check .`. If it fails, run `npx prettier --write .` and show the diff. Prettier applies
   `.editorconfig` (indentation, line endings) to the files it formats; `dotnet format` does the same for C#. Files
   neither formats (`.csproj`, `.props`, `.http`, dotfiles) rely on the editor, and git normalizes their line endings.

Report each step as one line: pass, fail or skipped. On a failure, include the relevant output.
