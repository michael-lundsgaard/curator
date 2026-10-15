---
name: verify
description: Build, format-check and test the Curator API before calling a step done or committing. Use when the user asks to verify, check or "make sure it works", or before proposing a commit.
---

Run from `apps/api`, stopping at the first failure:

1. `dotnet build Curator.slnx -v q`
2. `dotnet format Curator.slnx --verify-no-changes`. If it fails, run `dotnet format Curator.slnx` and show the diff.
3. `dotnet test Curator.slnx --no-build`

If the build fails because `bin/` is locked by a running `Curator.Api` process, don't kill it. Re-run steps 1 and 3
with `--artifacts-path` pointing at a temp/scratchpad directory and drop `--no-build`.

Report each step as one line: pass or fail. On a failure, include the relevant output.
