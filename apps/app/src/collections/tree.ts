import { $api } from "../api/client";
import type { components } from "../api/schema";

export type CollectionItem = components["schemas"]["ListCollectionsItem"];

// The whole tree as GET /collections lists it: flat, with parentId, sorted by name. Shared by the sidebar, the search
// box and the collection pages (same query key). Fresh for a minute, so moving between collections doesn't fetch it
// again each time; coming back to the tab after that does, for clips others added.
export function useCollectionTree() {
  return $api.useQuery("get", "/collections", {}, { staleTime: 60_000 });
}

export function childrenOf(items: CollectionItem[], parentId: string | null) {
  return items.filter((item) => item.parentId === parentId);
}

// Every collection, each followed by its sub-collections, with how deep it is (0 at the top level). Keeps the API's
// order by name within each level. Grouped by parent first, so it's one pass however big the tree.
export function flatten(items: CollectionItem[]) {
  const byParent = new Map<string | null, CollectionItem[]>();
  for (const item of items) {
    const siblings = byParent.get(item.parentId);
    if (siblings) siblings.push(item);
    else byParent.set(item.parentId, [item]);
  }
  const result: { item: CollectionItem; depth: number }[] = [];
  const visit = (parentId: string | null, depth: number) => {
    for (const item of byParent.get(parentId) ?? []) {
      result.push({ item, depth });
      visit(item.id, depth + 1);
    }
  };
  visit(null, 0);
  return result;
}

// The collections above this one, the top-level one first. Empty at the top level or when the id isn't in the tree.
export function ancestorsOf(items: CollectionItem[], id: string) {
  const byId = new Map(items.map((item) => [item.id, item]));
  const ancestors: CollectionItem[] = [];
  let parentId = byId.get(id)?.parentId ?? null;
  // The API never makes a cycle; the length check keeps a bad response from looping.
  while (parentId && ancestors.length < items.length) {
    const parent = byId.get(parentId);
    if (!parent) break;
    ancestors.unshift(parent);
    parentId = parent.parentId;
  }
  return ancestors;
}
