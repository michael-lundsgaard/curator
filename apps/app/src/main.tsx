import { createRouter, RouterProvider } from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { auth } from "./auth/client";
import "./index.css";
import { routeTree } from "./routeTree.gen";

const router = createRouter({ routeTree });

// Signed out from the account menu, in another tab, or because the session couldn't be refreshed: running the
// routes' beforeLoad again sends a signed-in page to sign in, and the next user's profile is loaded fresh.
auth.onAuthStateChange((event) => {
  if (event === "SIGNED_OUT") void router.invalidate();
});

// Makes Link, navigate and useSearch type-checked against the routes.
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
