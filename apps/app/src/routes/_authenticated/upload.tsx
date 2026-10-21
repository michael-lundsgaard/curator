import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated/upload")({
  component: () => <h1 className="text-[34px] font-extrabold tracking-[-0.02em]">Upload</h1>,
});
