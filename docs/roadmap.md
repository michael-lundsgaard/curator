# Roadmap

Built step by step. Each step is small and shippable. Order may change.

- [x] API foundation and tooling
- [x] Choose hosting for database and auth → [0001](decisions/0001-supabase-for-postgres-and-auth.md)
- [ ] Settle the open questions in [data-model.md](data-model.md)
- [ ] EF Core + Npgsql: `profiles` + `assets`, first migration, database health check
- [ ] Authentication: validate Supabase JWTs, create the profile on first request
- [ ] Uploads: create asset, presigned R2 upload URL, mark as uploaded
- [ ] Collections: tree, add/remove clips, ordering, cover image
- [ ] React app (`apps/app`, Vite): sign-in, generated API client, browse and upload
- [ ] Video processing: transcode to playback version and generate thumbnail
- [ ] Invitations (disable sign-up, `inviteUserByEmail`)
- [ ] Share links
