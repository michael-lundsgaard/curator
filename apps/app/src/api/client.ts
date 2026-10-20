import createClient from "openapi-fetch";
import type { paths } from "./schema";

// The types come from apps/api/openapi.json (`npm run generate:api`). Requests go to /api on the app's own
// origin; the Vite dev server will proxy that to the API.
export const api = createClient<paths>({ baseUrl: "/api" });
