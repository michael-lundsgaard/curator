import type { components } from "../api/schema";

export type AssetSort = components["schemas"]["AssetSort"];
export type SortOrder = components["schemas"]["SortOrder"];

// What a clip listing (the library, a collection) keeps in its URL, so a search can be linked to and Back undoes a
// filter. Missing means the API's default; the top bar's search box sets q.
export type ClipSearch = {
  q?: string;
  uploadedBy?: string;
  sort?: AssetSort;
  order?: SortOrder;
};

// The sorts a clip listing offers, the API's default first.
export const sorts: { sort: AssetSort; order: SortOrder; label: string }[] = [
  { sort: "Recorded", order: "Desc", label: "Recorded, newest first" },
  { sort: "Recorded", order: "Asc", label: "Recorded, oldest first" },
  { sort: "Uploaded", order: "Desc", label: "Uploaded, newest first" },
  { sort: "Uploaded", order: "Asc", label: "Uploaded, oldest first" },
  { sort: "Title", order: "Asc", label: "Title, A to Z" },
  { sort: "Title", order: "Desc", label: "Title, Z to A" },
];

// Reads a route's search params. Anything unknown is left out.
export function parseClipSearch(search: Record<string, unknown>): ClipSearch {
  const sort = sorts.find((s) => s.sort === search.sort)?.sort;
  const order =
    sort && (search.order === "Asc" || search.order === "Desc") ? search.order : undefined;
  return {
    q: typeof search.q === "string" && search.q.trim() ? search.q : undefined,
    uploadedBy: typeof search.uploadedBy === "string" ? search.uploadedBy : undefined,
    sort,
    order,
  };
}
