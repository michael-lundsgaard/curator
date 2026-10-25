import { useEffect, useId, useRef, type ReactNode } from "react";

// A modal <dialog>, open for as long as it's rendered: the browser puts it above the page, moves focus into it, keeps
// focus there and closes it on Escape, which calls onClose. The parent stops rendering it to close it.
export function Dialog({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  // No close() on unmount: removing the element closes it, and a close() would fire onClose a moment later, after
  // React's development double mount has opened it again. The check skips that second showModal().
  // showModal() focuses the first field or button; one marked data-autofocus instead, such as "Keep it" before a
  // Delete, so Enter doesn't delete. (React's autoFocus doesn't set the attribute showModal() looks for.)
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog || dialog.open) return;
    dialog.showModal();
    dialog.querySelector<HTMLElement>("[data-autofocus]")?.focus();
  }, []);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={onClose}
      className="m-auto w-[calc(100%-2rem)] max-w-120 rounded-[14px] border border-line bg-surface p-6 text-text backdrop:bg-black/50"
    >
      <div className="flex flex-col gap-4">
        <h2 id={titleId} className="text-[22px] font-extrabold tracking-[-0.02em] break-words">
          {title}
        </h2>
        {children}
      </div>
    </dialog>
  );
}
