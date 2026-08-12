"use client";
import type { LucideIcon } from "lucide-react";
import {
  AlertTriangle,
  BookOpen,
  CheckCircle2,
  Clock3,
  MoreHorizontal,
  Play,
  Plus,
  Search,
  UploadCloud,
  Video,
} from "lucide-react";

import {
  MediaStatus,
  type Lesson,
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
import { Input } from "@/shared/components/ui/input";
import { useState } from "react";
import { lessonsApi } from "@/app/entities/lessons/api";
import { error } from "console";

const lessons: Lesson[] = [
  {
    id: "lesson-01",
    title: "Введение в современный Frontend",
    description:
      "Разберём устройство веб-приложений и подготовим окружение для дальнейшей работы.",
    video: {
      id: "media-01",
      url: "#",
      status: MediaStatus.READY,
    },
    createdAt: new Date("2026-08-02"),
    updatedAt: new Date("2026-08-10"),
  },
  {
    id: "lesson-02",
    title: "Компоненты и композиция",
    description:
      "Учимся разбивать интерфейс на независимые компоненты и переиспользовать их.",
    video: {
      id: "media-02",
      url: "#",
      status: MediaStatus.READY,
    },
    createdAt: new Date("2026-08-04"),
    updatedAt: new Date("2026-08-11"),
  },
  {
    id: "lesson-03",
    title: "Работа с данными в интерфейсе",
    description:
      "Подготовим типы данных и научимся отображать коллекции в удобном формате.",
    video: {
      id: "media-03",
      url: "#",
      status: MediaStatus.UPLOADED,
    },
    createdAt: new Date("2026-08-06"),
    updatedAt: new Date("2026-08-12"),
  },
  {
    id: "lesson-04",
    title: "Адаптивная вёрстка",
    description:
      "Соберём макет, который одинаково хорошо выглядит на телефоне и компьютере.",
    video: {
      id: "media-04",
      url: "#",
      status: MediaStatus.UPLOADING,
    },
    createdAt: new Date("2026-08-08"),
    updatedAt: new Date("2026-08-12"),
  },
  {
    id: "lesson-05",
    title: "Формы и пользовательский ввод",
    description:
      "Проектируем понятные формы, состояния полей и обратную связь для пользователя.",
    video: {
      id: "media-05",
      url: "#",
      status: MediaStatus.FAILED,
    },
    createdAt: new Date("2026-08-09"),
    updatedAt: new Date("2026-08-12"),
  },
  {
    id: "lesson-06",
    title: "Финальная сборка проекта",
    description:
      "Объединим изученные приёмы, проверим результат и подготовим приложение к запуску.",
    createdAt: new Date("2026-08-12"),
    updatedAt: new Date("2026-08-12"),
  },
];

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

const PAGE_SIZE = 10;

export default function LessonsPage() {
  const [page, setPage] = useState(1);
  const [lessons, setLessons] = useState<Lesson[]>([]);

  lessonsApi
    .getLessons({ page, pageSize: PAGE_SIZE })
    .then((data) => setLessons(data))
    .catch((error) => console.error(error));

  const readyLessons = lessons.filter(
    (lesson) => lesson.video?.status === MediaStatus.READY,
  ).length;

  console.log(lessons);

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

        <Button className="h-10 w-full gap-2 px-4 sm:w-auto">
          <Plus className="size-4" aria-hidden="true" />
          Добавить урок
        </Button>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Всего уроков
          </p>
          <p className="mt-2 text-2xl font-bold">{lessons.length}</p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            Готовы к просмотру
          </p>
          <p className="mt-2 text-2xl font-bold text-emerald-300">
            {readyLessons}
          </p>
        </div>
        <div className="rounded-xl bg-card p-4 ring-1 ring-white/10">
          <p className="text-xs font-medium uppercase tracking-[0.16em] text-muted-foreground">
            В подготовке
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
            {lessons.length} материалов
          </span>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
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
                    УРОК {String(index + 1).padStart(2, "0")}
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
      </div>
    </section>
  );
}
