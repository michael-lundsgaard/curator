import type { QueryClient } from "@tanstack/react-query";
import { api } from "../api/client";
import type { ClipQuery } from "../library/ClipListing";

// The API takes up to 100 clips a request (AddClipsToCollection.MaxClips), and "Load more" can select more, so they're
// sent 100 at a time, one after another. Both skip clips already added or removed, so after a failure halfway the
// whole selection can be tried again. Each returns how many clips it changed.

const batchSize = 100;

async function inBatches(assetIds: string[], send: (batch: string[]) => Promise<number>) {
  let changed = 0;
  for (let i = 0; i < assetIds.length; i += batchSize) {
    changed += await send(assetIds.slice(i, i + batchSize));
  }
  return changed;
}

export function addClips(collectionId: string, assetIds: string[]) {
  return inBatches(assetIds, async (batch) => {
    const { data, error } = await api.POST("/collections/{id}/assets", {
      params: { path: { id: collectionId } },
      body: { assetIds: batch },
    });
    if (error) throw error;
    return data.addedCount;
  });
}

export function removeClips(collectionId: string, assetIds: string[]) {
  return inBatches(assetIds, async (batch) => {
    const { data, error } = await api.POST("/collections/{id}/assets/remove", {
      params: { path: { id: collectionId } },
      body: { assetIds: batch },
    });
    if (error) throw error;
    return data.removedCount;
  });
}

// After clips were added or removed: the tree's clip counts, and the clips of every collection, since a collection
// with its sub-collections' clips changes too. Not the library's listing, which stays the same.
export function refreshCollections(queryClient: QueryClient) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ["get", "/collections"] }),
    queryClient.invalidateQueries({
      queryKey: ["get", "/assets"],
      predicate: ({ queryKey }) => {
        const init = queryKey[2] as { params?: { query?: ClipQuery } } | undefined;
        return init?.params?.query?.collectionId !== undefined;
      },
    }),
  ]);
}
