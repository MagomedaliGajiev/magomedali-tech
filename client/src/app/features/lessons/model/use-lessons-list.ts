"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useEffect, useState } from "react";

import { lessonsApi } from "@/app/entities/lessons/api";

const PAGE_SIZE = 12;
const LESSONS_QUERY_KEY = ["lessons"] as const;

const lessonsQueryOptions = (page: number) =>
  queryOptions({
    queryKey: [...LESSONS_QUERY_KEY, { page, pageSize: PAGE_SIZE }],
    queryFn: ({ signal }) =>
      lessonsApi.getLessons({ page, pageSize: PAGE_SIZE }, signal),
  });

export function useLessonsList() {
  const [currentPage, setCurrentPage] = useState(1);
  const queryClient = useQueryClient();
  const { data, error, isFetching } = useQuery({
    ...lessonsQueryOptions(currentPage),
    placeholderData: keepPreviousData,
  });

  useEffect(() => {
    if (!data || currentPage >= data.totalPages) {
      return;
    }

    void queryClient.prefetchQuery(lessonsQueryOptions(currentPage + 1));
  }, [currentPage, data, queryClient]);

  const lessons = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = data?.totalPages ?? 0;
  const page = data?.page ?? currentPage;
  const pageSize = data?.pageSize ?? PAGE_SIZE;

  const refreshAfterLessonCreated = async () => {
    const lastPage = Math.max(1, Math.ceil((totalCount + 1) / PAGE_SIZE));

    setCurrentPage(lastPage);
    await queryClient.invalidateQueries({ queryKey: LESSONS_QUERY_KEY });
  };

  return {
    lessons,
    totalCount,
    totalPages,
    currentPage,
    page,
    pageSize,
    error,
    isFetching,
    setCurrentPage,
    refreshAfterLessonCreated,
  };
}
