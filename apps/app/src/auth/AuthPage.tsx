import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
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
