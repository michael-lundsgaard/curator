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

// How many levels collections can be nested: the API's Collection.MaxDepth.
export const maxDepth = 5;

// Every collection, each followed by its sub-collections, with how deep it is (0 at the top level). Keeps the API's
// order by name within each level. Grouped by parent first, so it's one pass however big the tree. With a parentId,
// only what's inside that collection, at every level, its children at depth 0.
export function flatten(items: CollectionItem[], parentId: string | null = null) {
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
  visit(parentId, 0);
  return result;
}

// How many collections are inside one at every level, for deleting it: "the 3 collections inside it". Not
// "sub-collections", which the collection's page counts only directly inside it.
export function insideCount(count: number) {
  return count === 1 ? "the collection inside it" : `the ${count} collections inside it`;
}

// The level a collection is at, as the API counts it: 1 at the top level, up to maxDepth.
export function levelOf(items: CollectionItem[], id: string) {
  return ancestorsOf(items, id).length + 1;
}

// How many levels a collection takes up with its sub-collections: 1 without any, 2 with only direct ones, and so on.
export function heightOf(items: CollectionItem[], id: string) {
  return Math.max(0, ...flatten(items, id).map(({ depth }) => depth + 1)) + 1;
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
