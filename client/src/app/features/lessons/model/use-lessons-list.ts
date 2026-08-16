"use client";

import {
  useInfiniteQuery,
  useQueryClient,
} from "@tanstack/react-query";

import { lessonsApi } from "@/app/entities/lessons/api";

const PAGE_SIZE = 12;
const LESSONS_QUERY_KEY = ["lessons"] as const;

export function useLessonsList() {
  const queryClient = useQueryClient();
  const query = useInfiniteQuery({
    queryKey: [...LESSONS_QUERY_KEY, { pageSize: PAGE_SIZE }],
    queryFn: ({ pageParam, signal }) =>
      lessonsApi.getLessons(
        { page: pageParam, pageSize: PAGE_SIZE },
        signal,
      ),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const nextPage = lastPage.page + 1;

      return nextPage <= lastPage.totalPages ? nextPage : undefined;
    },
  });

  const lessons = query.data?.pages.flatMap((page) => page.items) ?? [];
  const totalCount = query.data?.pages[0]?.totalCount ?? 0;

  const refreshAfterLessonCreated = async () => {
    await queryClient.invalidateQueries({ queryKey: LESSONS_QUERY_KEY });
  };

  return {
    ...query,
    lessons,
    totalCount,
    refreshAfterLessonCreated,
  };
}
