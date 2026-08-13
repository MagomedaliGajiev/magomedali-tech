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
  Search,
  UploadCloud,
  Video,
} from "lucide-react";

import {
  MediaStatus,
  type MediaStatus as MediaStatusType,
} from "@/app/entities/lessons/types";
import type { CreateLessonRequest } from "@/app/entities/lessons/api";
import { Button } from "@/shared/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/components/ui/card";
import { Input } from "@/shared/components/ui/input";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
} from "@/shared/components/ui/field";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/shared/components/ui/pagination";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/shared/components/ui/sheet";
import { lessonsApi } from "@/app/entities/lessons/api";
import {
  keepPreviousData,
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { type FormEvent, useEffect, useState } from "react";

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

const PAGE_SIZE = 12;
const LESSONS_QUERY_KEY = ["lessons"] as const;
const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const EMPTY_CREATE_LESSON_FORM: CreateLessonRequest = {
  title: "",
  description: "",
  videoId: "",
};

const lessonsQueryOptions = (page: number) =>
  queryOptions({
    queryKey: [...LESSONS_QUERY_KEY, { page, pageSize: PAGE_SIZE }],
    queryFn: ({ signal }) =>
      lessonsApi.getLessons({ page, pageSize: PAGE_SIZE }, signal),
  });

type PaginationEntry = number | "start-ellipsis" | "end-ellipsis";

const getPaginationEntries = (
  currentPage: number,
  totalPages: number,
): PaginationEntry[] => {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  if (currentPage <= 4) {
    return [1, 2, 3, 4, 5, "end-ellipsis", totalPages];
  }

  if (currentPage >= totalPages - 3) {
    return [
      1,
      "start-ellipsis",
      totalPages - 4,
      totalPages - 3,
      totalPages - 2,
      totalPages - 1,
      totalPages,
    ];
  }

  return [
    1,
    "start-ellipsis",
    currentPage - 1,
    currentPage,
    currentPage + 1,
    "end-ellipsis",
    totalPages,
  ];
};

export default function LessonsPage() {
  const [currentPage, setCurrentPage] = useState(1);
  const [isCreateSheetOpen, setIsCreateSheetOpen] = useState(false);
  const [createLessonForm, setCreateLessonForm] =
    useState<CreateLessonRequest>(EMPTY_CREATE_LESSON_FORM);
  const [createFormError, setCreateFormError] = useState<string | null>(null);
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
  const loadError = error
    ? error instanceof Error
      ? error.message
      : "Не удалось загрузить уроки"
    : null;

  const readyLessons = lessons.filter(
    (lesson) => lesson.video?.status === MediaStatus.READY,
  ).length;
  const totalPages = data?.totalPages ?? 0;
  const paginationEntries = getPaginationEntries(currentPage, totalPages);
  const isPreviousDisabled = currentPage === 1 || isFetching;
  const isNextDisabled =
    totalPages === 0 || currentPage >= totalPages || isFetching;

  const createLessonMutation = useMutation({
    mutationFn: (request: CreateLessonRequest) =>
      lessonsApi.createLesson(request),
    onSuccess: async () => {
      const lastPage = Math.max(1, Math.ceil((totalCount + 1) / PAGE_SIZE));

      setIsCreateSheetOpen(false);
      setCreateLessonForm(EMPTY_CREATE_LESSON_FORM);
      setCreateFormError(null);
      setCurrentPage(lastPage);

      await queryClient.invalidateQueries({ queryKey: LESSONS_QUERY_KEY });
    },
  });

  const handleCreateLesson = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const request = {
      title: createLessonForm.title.trim(),
      description: createLessonForm.description.trim(),
      videoId: createLessonForm.videoId.trim(),
    };

    if (!request.title || !request.description) {
      setCreateFormError("Заполните название и описание урока");
      return;
    }

    if (!UUID_PATTERN.test(request.videoId)) {
      setCreateFormError("Укажите корректный UUID загруженного видео");
      return;
    }

    setCreateFormError(null);
    createLessonMutation.mutate(request);
  };

  const handleCreateSheetOpenChange = (open: boolean) => {
    setIsCreateSheetOpen(open);

    if (!open) {
      setCreateLessonForm(EMPTY_CREATE_LESSON_FORM);
      setCreateFormError(null);
      createLessonMutation.reset();
    }
  };

  const goToPage = (page: number) => {
    if (isFetching || page === currentPage || page < 1 || page > totalPages) {
      return;
    }

    setCurrentPage(page);
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
        onOpenChange={handleCreateSheetOpenChange}
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

          <form
            className="flex min-h-0 flex-1 flex-col"
            onSubmit={handleCreateLesson}
          >
            <div className="flex-1 space-y-5 overflow-y-auto px-6 py-2">
              <Field>
                <FieldLabel htmlFor="create-lesson-title">
                  Название
                </FieldLabel>
                <Input
                  id="create-lesson-title"
                  value={createLessonForm.title}
                  maxLength={200}
                  required
                  autoFocus
                  onChange={(event) =>
                    setCreateLessonForm((form) => ({
                      ...form,
                      title: event.target.value,
                    }))
                  }
                />
              </Field>

              <Field>
                <FieldLabel htmlFor="create-lesson-description">
                  Описание
                </FieldLabel>
                <textarea
                  id="create-lesson-description"
                  className="min-h-28 w-full resize-y rounded-lg border border-input bg-transparent px-3 py-2 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
                  value={createLessonForm.description}
                  maxLength={2000}
                  required
                  onChange={(event) =>
                    setCreateLessonForm((form) => ({
                      ...form,
                      description: event.target.value,
                    }))
                  }
                />
              </Field>

              <Field data-invalid={Boolean(createFormError)}>
                <FieldLabel htmlFor="create-lesson-video-id">
                  ID видео
                </FieldLabel>
                <Input
                  id="create-lesson-video-id"
                  value={createLessonForm.videoId}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  required
                  aria-invalid={Boolean(createFormError)}
                  onChange={(event) =>
                    setCreateLessonForm((form) => ({
                      ...form,
                      videoId: event.target.value,
                    }))
                  }
                />
                <FieldDescription>
                  Видео должно быть заранее загружено в файловый сервис.
                </FieldDescription>
              </Field>

              {createFormError ? (
                <FieldError>{createFormError}</FieldError>
              ) : null}

              {createLessonMutation.error ? (
                <FieldError>{createLessonMutation.error.message}</FieldError>
              ) : null}
            </div>

            <SheetFooter className="border-t border-white/10 px-6 py-5">
              <Button
                type="submit"
                disabled={createLessonMutation.isPending}
              >
                {createLessonMutation.isPending ? (
                  <LoaderCircle className="size-4 animate-spin" />
                ) : (
                  <Plus className="size-4" />
                )}
                {createLessonMutation.isPending
                  ? "Создание…"
                  : "Создать урок"}
              </Button>
              <Button
                type="button"
                variant="outline"
                disabled={createLessonMutation.isPending}
                onClick={() => handleCreateSheetOpenChange(false)}
              >
                Отмена
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Всего уроков
          </p>
          <p className="mt-2 text-2xl font-bold">{totalCount}</p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Готовы на странице
          </p>
          <p className="mt-2 text-2xl font-bold text-emerald-300">
            {readyLessons}
          </p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            В подготовке на странице
          </p>
          <p className="mt-2 text-2xl font-bold text-amber-300">
            {lessons.length - readyLessons}
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-3 rounded-xl bg-card p-3 ring-1 ring-white/10 md:flex-row md:items-center">
        <div className="relative min-w-0 flex-1">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            className="h-10 bg-background/40 pl-9"
            placeholder="Найти урок по названию…"
            aria-label="Поиск уроков"
          />
        </div>
        <div className="flex gap-1 overflow-x-auto rounded-lg bg-background/40 p-1">
          <Button size="sm" className="shrink-0 px-3">
            Все
          </Button>
          <Button size="sm" variant="ghost" className="shrink-0 px-3">
            Готовы
          </Button>
          <Button size="sm" variant="ghost" className="shrink-0 px-3">
            В обработке
          </Button>
        </div>
      </div>

      <div>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold tracking-[-0.02em]">
            Все уроки
          </h2>
          <span className="text-sm text-muted-foreground">
            {isFetching ? "Загрузка…" : `${totalCount} материалов`}
          </span>
        </div>

        {loadError ? (
          <div
            className="mb-4 rounded-xl bg-red-500/10 p-4 text-sm text-red-300 ring-1 ring-red-400/20"
            role="alert"
          >
            Не удалось загрузить уроки: {loadError}
          </div>
        ) : null}

        <div
          id="lessons-list"
          className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          aria-busy={isFetching}
        >
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
                    {String(
                      ((data?.page ?? currentPage) - 1) *
                        (data?.pageSize ?? PAGE_SIZE) +
                        index +
                        1,
                    ).padStart(2, "0")}
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

        {totalPages > 1 ? (
          <Pagination className="mt-6">
            <PaginationContent>
              <PaginationItem>
                <PaginationPrevious
                  href="#lessons-list"
                  text="Назад"
                  aria-label="Перейти на предыдущую страницу"
                  aria-disabled={isPreviousDisabled}
                  tabIndex={isPreviousDisabled ? -1 : undefined}
                  className={
                    isPreviousDisabled
                      ? "pointer-events-none opacity-50"
                      : undefined
                  }
                  onClick={(event) => {
                    event.preventDefault();
                    goToPage(currentPage - 1);
                  }}
                />
              </PaginationItem>

              {paginationEntries.map((entry) => (
                <PaginationItem key={entry}>
                  {typeof entry === "number" ? (
                    <PaginationLink
                      href="#lessons-list"
                      isActive={entry === currentPage}
                      aria-label={`Перейти на страницу ${entry}`}
                      onClick={(event) => {
                        event.preventDefault();
                        goToPage(entry);
                      }}
                    >
                      {entry}
                    </PaginationLink>
                  ) : (
                    <PaginationEllipsis />
                  )}
                </PaginationItem>
              ))}

              <PaginationItem>
                <PaginationNext
                  href="#lessons-list"
                  text="Вперёд"
                  aria-label="Перейти на следующую страницу"
                  aria-disabled={isNextDisabled}
                  tabIndex={isNextDisabled ? -1 : undefined}
                  className={
                    isNextDisabled
                      ? "pointer-events-none opacity-50"
                      : undefined
                  }
                  onClick={(event) => {
                    event.preventDefault();
                    goToPage(currentPage + 1);
                  }}
                />
              </PaginationItem>
            </PaginationContent>
          </Pagination>
        ) : null}
      </div>
    </section>
  );
}
