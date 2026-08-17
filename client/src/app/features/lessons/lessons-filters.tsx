"use client";

import { LoaderCircle, RotateCcw, Search } from "lucide-react";
import { useShallow } from "zustand/react/shallow";

import {
  DEFAULT_LESSONS_FILTERS,
  type LessonPageSize,
  LESSON_PAGE_SIZES,
  LESSONS_SEARCH_MAX_LENGTH,
  useLessonsFiltersStore,
} from "@/app/features/lessons/model/lessons-filters-store";
import { Button } from "@/shared/components/ui/button";
import { Input } from "@/shared/components/ui/input";

type LessonsFiltersProps = {
  isFetching: boolean;
};

export function LessonsFilters({ isFetching }: LessonsFiltersProps) {
  const {
    search,
    isDeleted,
    pageSize,
    setSearch,
    setIsDeleted,
    setPageSize,
    reset,
  } = useLessonsFiltersStore(
    useShallow((state) => ({
      search: state.search,
      isDeleted: state.isDeleted,
      pageSize: state.pageSize,
      setSearch: state.setSearch,
      setIsDeleted: state.setIsDeleted,
      setPageSize: state.setPageSize,
      reset: state.reset,
    })),
  );

  const hasChangedFilters =
    search !== DEFAULT_LESSONS_FILTERS.search ||
    isDeleted !== DEFAULT_LESSONS_FILTERS.isDeleted ||
    pageSize !== DEFAULT_LESSONS_FILTERS.pageSize;

  const handlePageSizeChange = (value: string) => {
    const nextPageSize = Number(value) as LessonPageSize;

    if (LESSON_PAGE_SIZES.includes(nextPageSize)) {
      setPageSize(nextPageSize);
    }
  };

  return (
    <div
      className="flex flex-col gap-3 rounded-xl bg-card p-3 ring-1 ring-white/10 lg:flex-row lg:items-center"
      aria-label="Фильтры уроков"
    >
      <div className="relative min-w-0 flex-1">
        <Search
          className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          className="h-10 bg-background/40 pl-9 pr-10"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          maxLength={LESSONS_SEARCH_MAX_LENGTH}
          placeholder="Найти урок по названию…"
          aria-label="Поиск уроков"
          autoComplete="off"
        />
        {isFetching ? (
          <LoaderCircle
            className="absolute right-3 top-1/2 size-4 -translate-y-1/2 animate-spin text-muted-foreground"
            aria-label="Обновление списка"
          />
        ) : null}
      </div>

      <div
        className="flex gap-1 overflow-x-auto rounded-lg bg-background/40 p-1"
        aria-label="Статус удаления"
      >
        <Button
          type="button"
          size="sm"
          variant={isDeleted ? "ghost" : "default"}
          className="shrink-0 px-3"
          aria-pressed={!isDeleted}
          onClick={() => setIsDeleted(false)}
        >
          Активные
        </Button>
        <Button
          type="button"
          size="sm"
          variant={isDeleted ? "default" : "ghost"}
          className="shrink-0 px-3"
          aria-pressed={isDeleted}
          onClick={() => setIsDeleted(true)}
        >
          Удалённые
        </Button>
      </div>

      <label className="flex h-10 shrink-0 items-center gap-2 rounded-lg bg-background/40 px-3 text-sm text-muted-foreground">
        На странице
        <select
          className="bg-transparent font-medium text-foreground outline-none"
          value={pageSize}
          onChange={(event) => handlePageSizeChange(event.target.value)}
        >
          {LESSON_PAGE_SIZES.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>

      {hasChangedFilters ? (
        <Button
          type="button"
          variant="ghost"
          className="h-10 gap-2"
          onClick={reset}
        >
          <RotateCcw className="size-4" aria-hidden="true" />
          Сбросить
        </Button>
      ) : null}
    </div>
  );
}
