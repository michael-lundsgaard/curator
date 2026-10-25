import { createFileRoute, Link, redirect, useNavigate } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { AuthPage, cardClass } from "../auth/AuthPage";
import { auth } from "../auth/client";
import { authErrorMessage } from "../auth/errors";
import { FormError } from "../ui/alerts";
import { Button } from "../ui/Button";
import { TextField } from "../ui/fields";

type SignInSearch = { redirect?: string };

export const Route = createFileRoute("/sign-in")({
  // Where to go after signing in: the page that sent the user here. Only a path in the app, never another site.
  validateSearch: (search): SignInSearch => {
    const target = search.redirect;
    return typeof target === "string" && target.startsWith("/") && !target.startsWith("//")
      ? { redirect: target }
      : {};
  },
  async beforeLoad({ search }) {
    const { data } = await auth.getSession();
    if (data.session) throw redirect({ href: search.redirect ?? "/" });
  },
  component: SignIn,
});

function SignIn() {
  const search = Route.useSearch();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setPending(true);
    setError(null);
    const { error } = await auth.signInWithPassword({
      email: String(form.get("email")),
      password: String(form.get("password")),
    });
    setPending(false);
    if (error) setError(authErrorMessage(error));
    else await navigate({ href: search.redirect ?? "/" });
  }

  return (
    <AuthPage>
      <div className="flex max-w-130 flex-[1_1_380px] flex-col gap-5">
        <p className="font-mono text-[13px] tracking-[0.08em] text-meta uppercase">
          Keep. Find. Show.
        </p>
        <h1 className="text-[40px] leading-[1.02] font-extrabold tracking-[-0.03em] sm:text-[56px]">
          Every clip, in its place.
        </h1>
        <p className="text-lg leading-normal text-meta">
          Upload once, sort into collections and find any clip in seconds.
        </p>
      </div>

      <form onSubmit={onSubmit} className={`${cardClass} flex-[1_1_360px]`}>
        <div className="flex flex-col gap-1.5">
          <h2 className="text-[26px] font-extrabold tracking-[-0.02em]">Sign in</h2>
          <p className="text-[15px] text-meta">Welcome back.</p>
        </div>
        {error && <FormError>{error}</FormError>}
        <TextField
          id="email"
          label="Email"
          type="email"
          required
          autoComplete="email"
          placeholder="you@example.com"
        />
        <TextField
          id="password"
          label="Password"
          type="password"
          required
          autoComplete="current-password"
          aside={
            <Link
              to="/reset-password"
              className="text-sm font-semibold text-link hover:text-accent-hover"
            >
              Forgot password?
            </Link>
          }
        />
        <Button type="submit" variant="primary" size="xl" disabled={pending}>
          {pending ? "Signing in…" : "Sign in"}
        </Button>
        <p className="border-t border-line pt-4 text-sm leading-normal text-meta">
          Curator is invite only. Ask an admin to invite you.
        </p>
      </form>
    </AuthPage>
  );
}
