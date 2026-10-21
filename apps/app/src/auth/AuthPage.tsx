import { Link } from "@tanstack/react-router";
import type { ComponentProps, ReactNode } from "react";
import { Logo } from "../shell/Logo";
import { ThemeSwitch } from "../shell/ThemeSwitch";

// The frame of the pages outside the shell (sign in, reset password): the logo and the theme switch above, the page
// centered below.
export function AuthPage({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col px-4 py-6">
      <header className="mx-auto flex w-full max-w-300 items-center justify-between">
        <Link to="/sign-in" className="flex items-center gap-2.5 text-text">
          <Logo />
          <span className="text-[21px] font-extrabold tracking-[-0.03em]">Curator</span>
        </Link>
        <ThemeSwitch />
      </header>
      <main className="mx-auto flex w-full max-w-300 flex-1 flex-wrap items-center justify-center gap-16 py-12">
        {children}
      </main>
    </div>
  );
}

export const cardClass =
  "flex w-full max-w-105 flex-col gap-5 rounded-2xl border border-line bg-surface p-6 sm:p-9";

export const primaryButtonClass =
  "flex h-12 cursor-pointer items-center justify-center rounded-[10px] bg-accent text-base font-semibold text-white hover:bg-accent-hover disabled:cursor-default disabled:opacity-60";

export function Field({
  id,
  label,
  aside,
  ...input
}: { id: string; label: string; aside?: ReactNode } & ComponentProps<"input">) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between">
        <label htmlFor={id} className="text-sm font-semibold">
          {label}
        </label>
        {aside}
      </div>
      <input
        id={id}
        name={id}
        required
        className="h-11 rounded-[10px] border border-line bg-bg px-3 text-[15px] placeholder:text-meta"
        {...input}
      />
    </div>
  );
}

export function FormError({ children }: { children: ReactNode }) {
  return (
    <p role="alert" className="rounded-[10px] bg-danger-soft px-3 py-2.5 text-sm text-danger">
      {children}
    </p>
  );
}
