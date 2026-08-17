"use client";
import type { LucideIcon } from "lucide-react";
import {
  AlertTriangle,
  BookOpen,
  CheckCircle2,
  Clock3,
  LoaderCircle,
  MoreHorizontal,
  Play,
  Plus,
  UploadCloud,
  Video,
} from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useShallow } from "zustand/react/shallow";

import {
  MediaStatus,
  type MediaStatus as MediaStatusType,
} from "@/app/entities/lessons/types";
import { Button } from "@/shared/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
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

const statusMeta: Record<
  MediaStatusType,
  { label: string; icon: LucideIcon; className: string }
> = {
  [MediaStatus.READY]: {
    label: "Готово",
    icon: CheckCircle2,
    className: "bg-emerald-500/15 text-emerald-300 ring-emerald-400/20",
  },
  [MediaStatus.UPLOADED]: {
    label: "Обработка",
    icon: Clock3,
    className: "bg-sky-500/15 text-sky-300 ring-sky-400/20",
  },
  [MediaStatus.UPLOADING]: {
    label: "Загрузка",
    icon: UploadCloud,
    className: "bg-amber-500/15 text-amber-300 ring-amber-400/20",
  },
  [MediaStatus.FAILED]: {
    label: "Ошибка",
    icon: AlertTriangle,
    className: "bg-red-500/15 text-red-300 ring-red-400/20",
  },
  [MediaStatus.DELETED]: {
    label: "Удалено",
    icon: AlertTriangle,
    className: "bg-zinc-500/15 text-zinc-300 ring-zinc-400/20",
  },
};

const lessonGradients = [
  "from-rose-500/30 via-red-500/10 to-zinc-950",
  "from-blue-500/30 via-indigo-500/10 to-zinc-950",
  "from-violet-500/30 via-fuchsia-500/10 to-zinc-950",
  "from-amber-500/30 via-orange-500/10 to-zinc-950",
  "from-cyan-500/30 via-teal-500/10 to-zinc-950",
  "from-emerald-500/30 via-green-500/10 to-zinc-950",
];

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

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
              Заполните информацию и укажите ID ранее загруженного видео.
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
            {lessons.length - readyLessons}
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

          {lessons.map((lesson, index) => {
            const mediaStatus = lesson.video?.status;
            const status = mediaStatus ? statusMeta[mediaStatus] : null;
            const StatusIcon = status?.icon;

            return (
              <Card
                key={lesson.id}
                className="gap-0 border-0 py-0 transition-transform duration-200 hover:-translate-y-1 hover:ring-white/20"
              >
                <div
                  className={`relative flex aspect-video items-center justify-center overflow-hidden bg-gradient-to-br ${lessonGradients[index % lessonGradients.length]}`}
                >
                  <div className="absolute inset-0 bg-[radial-gradient(circle_at_70%_20%,rgba(255,255,255,0.14),transparent_35%)]" />
                  <span className="absolute left-4 top-4 font-mono text-xs font-semibold text-white/50">
                    УРОК{" "}
                    {String(index + 1).padStart(2, "0")}
                  </span>

                  {mediaStatus === MediaStatus.READY ? (
                    <span className="relative flex size-14 items-center justify-center rounded-full bg-white text-black shadow-2xl transition-transform group-hover/card:scale-105">
                      <Play
                        className="size-5 translate-x-0.5 fill-current"
                        aria-hidden="true"
                      />
                    </span>
                  ) : (
                    <Video
                      className="relative size-11 text-white/45"
                      aria-hidden="true"
                    />
                  )}

                  {status ? (
                    <span
                      className={`absolute bottom-3 left-3 inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ${status.className}`}
                    >
                      {StatusIcon ? (
                        <StatusIcon className="size-3.5" aria-hidden="true" />
                      ) : null}
                      {status.label}
                    </span>
                  ) : (
                    <span className="absolute bottom-3 left-3 inline-flex items-center gap-1.5 rounded-full bg-zinc-500/15 px-2.5 py-1 text-xs font-medium text-zinc-300 ring-1 ring-zinc-400/20">
                      <Video className="size-3.5" aria-hidden="true" />
                      Без видео
                    </span>
                  )}
                </div>

                <CardHeader className="gap-2 p-4 pb-2">
                  <CardTitle className="line-clamp-2 text-lg font-semibold tracking-[-0.025em]">
                    {lesson.title}
                  </CardTitle>
                </CardHeader>
                <CardContent className="flex-1 px-4 pb-4">
                  <p className="line-clamp-2 text-sm leading-5 text-muted-foreground">
                    {lesson.description}
                  </p>
                </CardContent>
                <CardFooter className="justify-between border-white/10 bg-white/[0.025] px-4 py-3">
                  <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    <Clock3 className="size-3.5" aria-hidden="true" />
                    {dateFormatter.format(lesson.createdAt)}
                  </span>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`Действия с уроком «${lesson.title}»`}
                  >
                    <MoreHorizontal className="size-4" aria-hidden="true" />
                  </Button>
                </CardFooter>
              </Card>
            );
          })}
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
