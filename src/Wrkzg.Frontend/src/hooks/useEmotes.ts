import { useEffect, useRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { emotesApi, type EmoteDto } from "../api/emotes";

export function useEmotes() {
  const queryClient = useQueryClient();
  const hasTriedRefresh = useRef(false);

  const query = useQuery<EmoteDto[]>({
    queryKey: ["emotes"],
    queryFn: emotesApi.getAll,
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
  });

  // When cache is empty and we haven't tried yet: trigger backend refresh
  useEffect(() => {
    if (
      query.data !== undefined &&
      query.data.length === 0 &&
      !query.isLoading &&
      !hasTriedRefresh.current
    ) {
      hasTriedRefresh.current = true;
      emotesApi
        .refresh()
        .then((result) => {
          if (result.count > 0) {
            queryClient.invalidateQueries({ queryKey: ["emotes"] });
          }
        })
        .catch(() => {
          // Refresh failed — next attempt on auth change
        });
    }
  }, [query.data, query.isLoading, queryClient]);

  // Reset refresh flag when emotes become available (e.g. after auth change)
  useEffect(() => {
    if (query.data && query.data.length > 0) {
      hasTriedRefresh.current = false;
    }
  }, [query.data]);

  return query;
}

export function buildEmoteMap(emotes: EmoteDto[]): Map<string, EmoteDto> {
  const map = new Map<string, EmoteDto>();
  for (const emote of emotes) {
    map.set(emote.name, emote);
  }
  return map;
}
