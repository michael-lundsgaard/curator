import { AuthClient } from "@supabase/auth-js";

// Supabase Auth's client: the auth part of supabase-js, without the database, storage and realtime clients the app
// doesn't use (ADR 0012). The session lives in localStorage and is refreshed before it expires. Both values are
// public (apps/app/.env).
export const auth = new AuthClient({
  url: `${import.meta.env.VITE_SUPABASE_URL}/auth/v1`,
  headers: { apikey: import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY },
});
