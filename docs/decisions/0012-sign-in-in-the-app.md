# 0012: Sign-in in the app

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

[0001](0001-supabase-for-postgres-and-auth.md) has the app sign in with Supabase Auth and send the access token to
the API, which validates it ([0003](0003-authentication-and-permissions.md)). The app needs sign in, sign out and
reset password (the mocks in [design.md](../design.md)), and every other page needs a signed-in user. Sign-up stays
off: users join by invitation.

## Decision

### Client

- **`@supabase/auth-js`**, not all of `@supabase/supabase-js`. supabase-js wraps auth-js together with clients for
  the database, storage, realtime and functions, which the app never uses (the API is the only database client).
  `AuthClient` has the same methods as `supabase.auth`, so the Supabase docs still apply: `supabase.auth.x(...)` is
  `auth.x(...)` (`src/auth/client.ts`). The app's auth chunk is 48 kB gzipped against 81 kB with supabase-js.
- The Supabase URL and the publishable key are in a committed `apps/app/.env`. Both are public: the browser gets
  them anyway, and the key only identifies the project.
- The session is in `localStorage` and auth-js refreshes it. `src/api/client.ts` adds `Authorization: Bearer` to
  every API call, and a `401` from the API signs the user out.

### Routes

- Every page except sign in and reset password sits in a pathless layout route, `_authenticated.tsx`. Its
  `beforeLoad` sends a user without a session to `/sign-in?redirect=<page>`, and its loader fetches `GET /me` once
  for the account menu. URLs don't change (`/library` is still `/library`).
- `main.tsx` invalidates the router on `SIGNED_OUT` (from the account menu, another tab, or a refresh that failed),
  so the guard runs again and the next user's profile is loaded fresh.
- Sign out is `scope: "local"`: this browser only. Supabase's default ends the user's sessions on every device.

### Reset password

- **Implicit flow** (auth-js's default): the email link goes through Supabase and back to
  `/reset-password?step=new` with the session in the URL's hash. That works when the link is opened on another
  device or browser than the one that asked for it. The PKCE flow keeps a code verifier in the browser that asked,
  so the link would only work there; it's meant for server-rendered apps.
- `step=new` shows "Choose a new password" when the link gave a session, and "This link has expired" when not. The
  redirect URL has to be in Supabase's allow list.
- Asking for a link succeeds for unknown addresses too, so the form doesn't reveal who has an account.

## Alternatives considered

- **`@supabase/supabase-js`**, the usual choice and what the roadmap named. Nothing in it is wrong, but the app
  would download the four clients it doesn't use.
- **A React context holding the session**, read by the pages. The router's `beforeLoad` needs the session before
  rendering anyway, and `auth.getSession()` answers from `localStorage`, so the auth client is the one place that
  knows the session.
- **The `PASSWORD_RECOVERY` event** to tell a reset link apart from a signed-in user who opens `/reset-password`.
  It fires once, possibly before the page subscribes; `?step=new` in the redirect URL says the same thing in the URL.

## Consequences

- The Supabase project needs the app's URLs under _Authentication → URL Configuration_ (see the README), including
  the hosted app's when it's deployed.
- Supabase's built-in email sends only to the project team's addresses and a few emails an hour. Fine for trying
  resets; invitations will need custom SMTP.
- Changing the password while signed in, and the hosted app's origin, come with profile settings and hosting.
