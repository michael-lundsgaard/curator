import { createRootRoute, Outlet } from "@tanstack/react-router";

// Sign in and reset password render on their own; every other page sits in the shell of _authenticated.tsx.
export const Route = createRootRoute({
  component: Outlet,
});
