import { createRootRoute, Outlet } from "@tanstack/react-router";
import { Sidebar } from "../shell/Sidebar";
import { TopBar } from "../shell/TopBar";

// The shell around every page. Sign in and reset password will sit outside it, at that step.
export const Route = createRootRoute({
  component: () => (
    <div className="flex min-h-screen flex-col md:flex-row">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <main className="w-full max-w-300 p-4 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  ),
});
