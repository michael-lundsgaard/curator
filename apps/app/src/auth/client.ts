import { AuthClient } from "@supabase/auth-js";
import { supabasePublishableKey, supabaseUrl } from "./config";

// Supabase Auth's client: the auth part of supabase-js, without the database, storage and realtime clients the app
// doesn't use (ADR 0012). The session lives in localStorage and is refreshed before it expires.
export const auth = new AuthClient({
  url: `${supabaseUrl}/auth/v1`,
  headers: { apikey: supabasePublishableKey },
});
