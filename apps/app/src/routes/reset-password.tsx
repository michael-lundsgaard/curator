import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { Mail } from "lucide-react";
import { useState, type FormEvent } from "react";
import { AuthPage, cardClass, Field, FormError, primaryButtonClass } from "../auth/AuthPage";
import { auth } from "../auth/client";
import { authErrorMessage } from "../auth/errors";

type ResetPasswordSearch = { step?: "new" };

// The link in the email goes through Supabase and back to /reset-password?step=new with a session in the URL's hash,
// which the auth client reads and removes. That session may change the password.
const newPasswordUrl = () => `${location.origin}/reset-password?step=new`;

export const Route = createFileRoute("/reset-password")({
  validateSearch: (search): ResetPasswordSearch => (search.step === "new" ? { step: "new" } : {}),
  loaderDeps: ({ search }) => ({ step: search.step }),
  // getSession waits for the auth client to read the link's session. None means the link has expired or was used.
  async loader({ deps }) {
    if (deps.step !== "new") return { signedIn: false };
    const { data } = await auth.getSession();
    return { signedIn: data.session !== null };
  },
  component: ResetPassword,
});

function ResetPassword() {
  const { step: linkStep } = Route.useSearch();
  const { signedIn } = Route.useLoaderData();
  const [sent, setSent] = useState(false);
  const step = linkStep === "new" ? 3 : sent ? 2 : 1;

  return (
    <AuthPage>
      <div className={cardClass}>
        <div className="flex gap-1.5" aria-hidden="true">
          {[1, 2, 3].map((bar) => (
            <div
              key={bar}
              className={`h-1 flex-1 rounded-xs ${bar <= step ? "bg-accent" : "bg-line"}`}
            />
          ))}
        </div>
        {step === 1 && <RequestLink onSent={() => setSent(true)} />}
        {step === 2 && <CheckEmail onRetry={() => setSent(false)} />}
        {step === 3 && (signedIn ? <NewPassword /> : <LinkExpired />)}
      </div>
    </AuthPage>
  );
}

function RequestLink({ onSent }: { onSent: () => void }) {
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const email = String(new FormData(event.currentTarget).get("email"));
    setPending(true);
    setError(null);
    // Succeeds for unknown addresses too, so the form doesn't tell who has an account.
    const { error } = await auth.resetPasswordForEmail(email, { redirectTo: newPasswordUrl() });
    setPending(false);
    if (error) setError(authErrorMessage(error));
    else onSent();
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5">
      <Heading title="Reset your password">
        Enter the email you sign in with. We'll send you a link to choose a new password.
      </Heading>
      {error && <FormError>{error}</FormError>}
      <Field
        id="email"
        label="Email"
        type="email"
        autoComplete="email"
        placeholder="you@example.com"
      />
      <button type="submit" disabled={pending} className={primaryButtonClass}>
        {pending ? "Sending…" : "Send reset link"}
      </button>
      <BackToSignIn />
    </form>
  );
}

function CheckEmail({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="flex flex-col gap-5">
      <div className="flex size-12 items-center justify-center rounded-xl bg-accent-soft text-link">
        <Mail size={22} />
      </div>
      <Heading title="Check your email">
        If an account exists for that address, a reset link is on its way. The link works for one
        hour.
      </Heading>
      <p className="text-sm text-meta">
        No email? Check spam, or{" "}
        <button
          type="button"
          onClick={onRetry}
          className="cursor-pointer font-semibold text-link hover:text-accent-hover"
        >
          try again
        </button>
        .
      </p>
    </div>
  );
}

function NewPassword() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const password = String(form.get("new-password"));
    if (password !== form.get("confirm-password")) {
      setError("The passwords don't match.");
      return;
    }
    setPending(true);
    setError(null);
    const { error } = await auth.updateUser({ password });
    setPending(false);
    if (error) setError(authErrorMessage(error));
    else await navigate({ to: "/" });
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5">
      {/* 8 is also Supabase's minimum (Authentication → Sign In / Providers → Email). */}
      <Heading title="Choose a new password">At least 8 characters.</Heading>
      {error && <FormError>{error}</FormError>}
      <Field
        id="new-password"
        label="New password"
        type="password"
        autoComplete="new-password"
        minLength={8}
      />
      <Field
        id="confirm-password"
        label="Repeat new password"
        type="password"
        autoComplete="new-password"
        minLength={8}
      />
      <button type="submit" disabled={pending} className={primaryButtonClass}>
        {pending ? "Saving…" : "Save and sign in"}
      </button>
    </form>
  );
}

function LinkExpired() {
  return (
    <div className="flex flex-col gap-5">
      <Heading title="This link has expired">
        Reset links work once, for one hour. Request a new one and use the latest email.
      </Heading>
      <Link to="/reset-password" className={primaryButtonClass}>
        Request a new link
      </Link>
      <BackToSignIn />
    </div>
  );
}

function Heading({ title, children }: { title: string; children: string }) {
  return (
    <div className="flex flex-col gap-1.5">
      <h1 className="text-[26px] font-extrabold tracking-[-0.02em]">{title}</h1>
      <p className="text-[15px] leading-normal text-meta">{children}</p>
    </div>
  );
}

function BackToSignIn() {
  return (
    <Link
      to="/sign-in"
      className="self-center text-sm font-semibold text-link hover:text-accent-hover"
    >
      Back to sign in
    </Link>
  );
}
