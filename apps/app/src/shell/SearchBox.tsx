import { useMatchRoute, useNavigate, useSearch } from "@tanstack/react-router";
import { Search } from "lucide-react";
import { useEffect, useEffectEvent, useState, type FormEvent } from "react";

// Searches the library from any page: typing goes to /library?q=…, once the user pauses (or presses Enter). On the
// library itself the URL is replaced rather than pushed, so Back doesn't step through every pause.
export function SearchBox({ className }: { className?: string }) {
  const urlQuery = useSearch({ strict: false, select: (search) => search.q });
  const onLibrary = Boolean(useMatchRoute()({ to: "/library" }));
  const navigate = useNavigate();
  const [value, setValue] = useState(urlQuery ?? "");

  // The URL changed elsewhere (Back, "Show all clips"): show its query. Set during render, as React's docs suggest
  // for state that follows a prop, so the box never shows a stale value for a frame.
  const [shownQuery, setShownQuery] = useState(urlQuery);
  if (urlQuery !== shownQuery) {
    setShownQuery(urlQuery);
    setValue(urlQuery ?? "");
  }

  function search(text: string) {
    // Kept as typed, spaces too: trimming would change the box's value under the user while they type.
    const q = text.trim() ? text : undefined;
    if (q === urlQuery) return;
    void navigate({ to: "/library", search: (prev) => ({ ...prev, q }), replace: onLibrary });
  }

  // Only typing starts the timer: an effect event reads the latest URL and route without being a dependency.
  const searchAfterPause = useEffectEvent((text: string) => search(text));
  useEffect(() => {
    const timer = setTimeout(() => searchAfterPause(value), 300);
    return () => clearTimeout(timer);
  }, [value]);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    search(value);
  }

  return (
    <form role="search" onSubmit={onSubmit} className={`relative ${className ?? ""}`}>
      <Search
        size={18}
        aria-hidden="true"
        className="pointer-events-none absolute top-3.25 left-3 text-meta"
      />
      <input
        type="search"
        aria-label="Search clips"
        placeholder="Search titles and descriptions"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        className="h-11 w-full rounded-[10px] border border-line bg-surface pr-3 pl-10 text-[15px] placeholder:text-meta"
      />
    </form>
  );
}
