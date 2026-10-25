import { QueryClient } from "@tanstack/react-query";
import createClient from "openapi-fetch";
import createQueryHooks from "openapi-react-query";
import { auth } from "../auth/client";
import type { paths } from "./schema";

// The types come from apps/api/openapi.json (`npm run generate:api`). Requests go to /api on the app's own
// origin; the Vite dev server proxies that to the API.
export const api = createClient<paths>({ baseUrl: "/api" });

// TanStack Query hooks over the same client, typed the same way: $api.useQuery("get", "/profiles"). The query key
// is the method, path and parameters, so a change of parameters is a query of its own.
export const $api = createQueryHooks(api);

export const queryClient = new QueryClient();

api.use({
  // getSession refreshes the access token first when it has expired.
  async onRequest({ request }) {
    const { data } = await auth.getSession();
    if (data.session) request.headers.set("Authorization", `Bearer ${data.session.access_token}`);
    return request;
  },
  // The API rejected the token (say, the user was deleted in Supabase). Signing out sends the app to sign in, see
  // main.tsx.
  async onResponse({ response }) {
    if (response.status === 401) await auth.signOut({ scope: "local" });
    return response;
  },
});
