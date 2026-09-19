"use client";
import { BookOpen, LoaderCircle, Plus } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useShallow } from "zustand/react/shallow";

import { MediaStatus } from "@/app/entities/lessons/types";
import { LessonCard } from "@/app/features/lessons/lesson-card";
import { Button } from "@/shared/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
} from "@/shared/components/ui/card";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/shared/components/ui/sheet";
import { CreateLessonForm } from "@/app/features/lessons/create-lesson-form";
import { LessonsFilters } from "@/app/features/lessons/lessons-filters";
import { useLessonsFiltersStore } from "@/app/features/lessons/model/lessons-filters-store";
import { useLessonsList } from "@/app/features/lessons/model/use-lessons-list";
import { Skeleton } from "@/shared/components/ui/skeleton";

export default function LessonsPage() {
  const [isCreateSheetOpen, setIsCreateSheetOpen] = useState(false);
  const { search, isDeleted, resetFilters } = useLessonsFiltersStore(
    useShallow((state) => ({
      search: state.search,
      isDeleted: state.isDeleted,
      resetFilters: state.reset,
    })),
  );
  const {
    lessons,
    totalCount,
    error,
    fetchNextPage,
    hasNextPage,
    isFetching,
    isFetchingNextPage,
    isFetchNextPageError,
    isPending,
    refetch,
    refreshAfterLessonCreated,
    refreshLessons,
  } = useLessonsList();
  const observerRef = useRef<IntersectionObserver | null>(null);
  const hasResultFilters = search.trim().length > 0 || isDeleted;
  const loadError = error
    ? error instanceof Error
      ? error.message
      : "Не удалось загрузить уроки"
    : null;

  useEffect(() => {
    void useLessonsFiltersStore.persist.rehydrate();
  }, []);

  const loadMoreRef = useCallback(
    (node: HTMLDivElement | null) => {
      observerRef.current?.disconnect();
      observerRef.current = null;

      if (!node || !hasNextPage || isFetching) {
        return;
      }

      observerRef.current = new IntersectionObserver(
        ([entry]) => {
          if (entry?.isIntersecting) {
            void fetchNextPage();
          }
        },
        {
          rootMargin: "300px 0px",
          threshold: 0.5,
        },
      );
      observerRef.current.observe(node);
    },
    [fetchNextPage, hasNextPage, isFetching],
  );

  const readyLessons = lessons.filter(
    (lesson) => lesson.video?.status === MediaStatus.READY,
  ).length;
  const processingLessons = lessons.filter(
    (lesson) =>
      lesson.video?.status === MediaStatus.UPLOADING ||
      lesson.video?.status === MediaStatus.UPLOADED,
  ).length;

  const handleLessonCreated = async () => {
    setIsCreateSheetOpen(false);
    await refreshAfterLessonCreated();
  };

  return (
    <section className="space-y-7">
      <div className="flex flex-col gap-5 border-b border-white/10 pb-7 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-3 flex items-center gap-2 text-sm font-medium text-primary">
            <BookOpen className="size-4" aria-hidden="true" />
            Библиотека курса
          </div>
          <h1 className="text-3xl font-bold tracking-[-0.04em] sm:text-4xl">
            Уроки
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
            Создавайте материалы курса и следите за подготовкой видео в одном
            месте.
          </p>
        </div>

        <Button
          className="h-10 w-full gap-2 px-4 sm:w-auto"
          onClick={() => setIsCreateSheetOpen(true)}
        >
          <Plus className="size-4" aria-hidden="true" />
          Добавить урок
        </Button>
      </div>

      <Sheet
        open={isCreateSheetOpen}
        onOpenChange={setIsCreateSheetOpen}
      >
        <SheetContent className="w-full sm:max-w-md">
          <SheetHeader className="border-b border-white/10 p-6 pr-14">
            <SheetTitle className="text-xl font-semibold">
              Новый урок
            </SheetTitle>
            <SheetDescription>
              Заполните информацию. Видео можно загрузить сейчас или добавить
              позже.
            </SheetDescription>
          </SheetHeader>

          <CreateLessonForm
            onCancel={() => setIsCreateSheetOpen(false)}
            onLessonCreated={handleLessonCreated}
          />
        </SheetContent>
      </Sheet>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Найдено уроков
          </p>
          <p className="mt-2 text-2xl font-bold">{totalCount}</p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Готовы среди загруженных
          </p>
          <p className="mt-2 text-2xl font-bold text-emerald-300">
            {readyLessons}
          </p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            В подготовке среди загруженных
          </p>
          <p className="mt-2 text-2xl font-bold text-amber-300">
            {processingLessons}
          </p>
        </div>
      </div>

      <LessonsFilters isFetching={isFetching && !isFetchingNextPage} />

      <div>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-[-0.02em]">
            {isDeleted ? "Удалённые уроки" : "Активные уроки"}
          </h2>
          <span className="text-sm text-muted-foreground">
            {isPending
              ? "Загрузка…"
              : `${lessons.length} из ${totalCount} материалов`}
          </span>
        </div>

        {loadError && !isFetchNextPageError ? (
          <div
            className="mb-4 flex flex-col items-start gap-3 rounded-xl bg-red-500/10 p-4 text-sm text-red-300 ring-1 ring-red-400/20 sm:flex-row sm:items-center sm:justify-between"
            role="alert"
          >
            <span>Не удалось загрузить уроки: {loadError}</span>
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => void refetch()}
            >
              Повторить
            </Button>
          </div>
        ) : null}

        <div
          id="lessons-list"
          className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          aria-busy={isPending || isFetchingNextPage}
        >
          {isPending
            ? Array.from({ length: 6 }, (_, index) => (
                <Card
                  key={index}
                  className="gap-0 overflow-hidden border-0 py-0"
                  aria-hidden="true"
                >
                  <Skeleton className="aspect-video rounded-none" />
                  <CardHeader className="gap-3 p-4 pb-2">
                    <Skeleton className="h-5 w-4/5" />
                  </CardHeader>
                  <CardContent className="space-y-2 px-4 pb-4">
                    <Skeleton className="h-4 w-full" />
                    <Skeleton className="h-4 w-2/3" />
                  </CardContent>
                  <CardFooter className="border-white/10 px-4 py-3">
                    <Skeleton className="h-4 w-24" />
                  </CardFooter>
                </Card>
              ))
            : null}

          {lessons.map((lesson, index) => (
            <LessonCard
              key={lesson.id}
              lesson={lesson}
              index={index}
              canManage={!isDeleted}
              onLessonDeleted={refreshLessons}
              onVideoUpdated={refreshLessons}
            />
          ))}
        </div>

        {!isPending && lessons.length === 0 && !loadError ? (
          <div className="mt-6 flex flex-col items-center gap-3 rounded-xl bg-card p-8 text-center text-sm text-muted-foreground ring-1 ring-white/10">
            <span>
              {hasResultFilters
                ? "По заданным фильтрам уроков не найдено."
                : "Уроков пока нет. Добавьте первый материал курса."}
            </span>
            {hasResultFilters ? (
              <Button type="button" variant="outline" onClick={resetFilters}>
                Сбросить фильтры
              </Button>
            ) : null}
          </div>
        ) : null}

        {lessons.length > 0 ? (
          <div
            ref={loadMoreRef}
            className="mt-6 flex min-h-16 items-center justify-center"
            aria-live="polite"
          >
            {isFetchingNextPage ? (
              <span
                className="inline-flex items-center gap-2 text-sm text-muted-foreground"
                role="status"
              >
                <LoaderCircle
                  className="size-4 animate-spin"
                  aria-hidden="true"
                />
                Загружаем ещё уроки…
              </span>
            ) : isFetchNextPageError ? (
              <div className="flex flex-col items-center gap-2 text-sm text-red-300">
                <span>Не удалось загрузить следующую страницу.</span>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() => void fetchNextPage()}
                >
                  Повторить
                </Button>
              </div>
            ) : hasNextPage ? (
              <Button
                type="button"
                variant="ghost"
                onClick={() => void fetchNextPage()}
                disabled={isFetching}
              >
                Загрузить ещё
              </Button>
            ) : (
              <span className="text-sm text-muted-foreground">
                Все уроки загружены
              </span>
            )}
          </div>
        ) : null}
      </div>
    </section>
  );
}
