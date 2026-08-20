"use client";

import { useInfiniteQuery, useQueryClient } from "@tanstack/react-query";
import { useShallow } from "zustand/react/shallow";

import { lessonsApi } from "@/app/entities/lessons/api";
import { useLessonsFiltersStore } from "@/app/features/lessons/model/lessons-filters-store";
import { useDebounce } from "@/shared/hooks/use-debounce";

const LESSONS_QUERY_KEY = ["lessons"] as const;

export function useLessonsList() {
  const queryClient = useQueryClient();
  const { search, isDeleted, pageSize } = useLessonsFiltersStore(
    useShallow((state) => ({
      search: state.search,
      isDeleted: state.isDeleted,
      pageSize: state.pageSize,
    })),
  );
  const debouncedSearch = useDebounce(search.trim(), 300);
  const query = useInfiniteQuery({
    queryKey: [
      ...LESSONS_QUERY_KEY,
      { search: debouncedSearch, isDeleted, pageSize },
    ],
    queryFn: ({ pageParam, signal }) =>
      lessonsApi.getLessons(
        {
          search: debouncedSearch || undefined,
          isDeleted,
          page: pageParam,
          pageSize,
        },
        signal,
      ),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const nextPage = lastPage.page + 1;

      return nextPage <= lastPage.totalPages ? nextPage : undefined;
    },
    placeholderData: (previousData, previousQuery) => {
      const previousFilters = previousQuery?.queryKey[1] as
        | { isDeleted?: unknown; pageSize?: unknown }
        | undefined;

      return previousFilters?.isDeleted === isDeleted &&
        previousFilters.pageSize === pageSize
        ? previousData
        : undefined;
    },
  });

  const lessons = query.data?.pages.flatMap((page) => page.items) ?? [];
  const totalCount = query.data?.pages[0]?.totalCount ?? 0;

  const refreshLessons = async () => {
    await queryClient.invalidateQueries({ queryKey: LESSONS_QUERY_KEY });
  };

  return {
    ...query,
    lessons,
    totalCount,
    refreshAfterLessonCreated: refreshLessons,
    refreshLessons,
  };
}
