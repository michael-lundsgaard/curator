import { createFileRoute, Outlet, redirect } from "@tanstack/react-router";
import { api } from "../api/client";
import { auth } from "../auth/client";
import { Sidebar } from "../shell/Sidebar";
import { TopBar } from "../shell/TopBar";

// A pathless layout: the pages in _authenticated/ keep their URLs (/, /library), but need a signed-in user and get
// the shell around them.
export const Route = createFileRoute("/_authenticated")({
  async beforeLoad({ location }) {
    const { data } = await auth.getSession();
    if (!data.session) throw redirect({ to: "/sign-in", search: { redirect: location.href } });
  },
  // The profile, for the account menu. Loaded once; main.tsx invalidates it when the user signs out.
  async loader() {
    const { data, response } = await api.GET("/me");
    if (!data) throw new Error(`Couldn't load your profile (${response.status}).`);
    return data;
  },
  staleTime: Infinity,
  // No maximum width: a page that shouldn't stretch (a form, text) caps its own, see docs/design.md.
  component: () => (
    <div className="flex min-h-screen flex-col md:flex-row">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <main className="w-full p-4 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  ),
});
