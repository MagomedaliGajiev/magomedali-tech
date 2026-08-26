"use client";

import type { LucideIcon } from "lucide-react";
import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  Download,
  LoaderCircle,
  Play,
  Trash2,
  UploadCloud,
  Video,
} from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { lessonsApi } from "@/app/entities/lessons/api";
import { filesApi } from "@/app/entities/files/api";
import {
  MediaStatus,
  type Lesson,
  type MediaStatus as MediaStatusType,
} from "@/app/entities/lessons/types";
import { useDeleteLesson } from "@/app/features/lessons/model/use-delete-lesson";
import { VideoUploadDialog } from "@/app/features/videos/video-upload-dialog";
import { getErrorMessage } from "@/shared/api/errors";
import { Button, buttonVariants } from "@/shared/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/components/ui/dialog";

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

type LessonCardProps = {
  lesson: Lesson;
  index: number;
  canManage?: boolean;
  onLessonDeleted: () => void | Promise<void>;
  onVideoUpdated: () => void | Promise<void>;
};

export function LessonCard({
  lesson,
  index,
  canManage = true,
  onLessonDeleted,
  onVideoUpdated,
}: LessonCardProps) {
  const [isVideoDialogOpen, setIsVideoDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const deleteLessonMutation = useDeleteLesson({
    onSuccess: async () => {
      setIsDeleteDialogOpen(false);
      await onLessonDeleted();
    },
  });
  const mediaStatus = lesson.video?.status;
  const status = mediaStatus ? statusMeta[mediaStatus] : null;
  const StatusIcon = status?.icon;

  const handleUploadComplete = async (mediaAssetId: string) => {
    await lessonsApi.updateLessonVideo(lesson.id, mediaAssetId);

    if (lesson.video?.id && lesson.video.id !== mediaAssetId) {
      try {
        await filesApi.deleteMediaAsset(lesson.video.id);
      } catch (error: unknown) {
        console.error("Не удалось удалить заменённое видео", error);
        toast.warning("Новое видео подключено, но старое не удалось удалить");
      }
    }

    await onVideoUpdated();
    toast.success("Видео урока обновлено");
  };

  const handleDeleteDialogChange = (open: boolean) => {
    if (deleteLessonMutation.isPending) {
      return;
    }

    if (!open) {
      deleteLessonMutation.reset();
    }

    setIsDeleteDialogOpen(open);
  };

  const handleDeleteLesson = async () => {
    try {
      await deleteLessonMutation.mutateAsync(lesson.id);
    } catch {
      // Ошибка показана в диалоге и через toast в mutation hook.
    }
  };

  return (
    <>
      <Card className="group/card gap-0 border-0 py-0 transition-transform duration-200 hover:-translate-y-1 hover:ring-white/20">
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
        <CardFooter className="flex-wrap justify-between gap-2 border-white/10 bg-white/[0.025] px-4 py-3">
          <span className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
            <Clock3 className="size-3.5 shrink-0" aria-hidden="true" />
            {dateFormatter.format(lesson.createdAt)}
          </span>
          {lesson.video?.url || canManage ? (
            <span className="flex items-center gap-1.5">
              {lesson.video?.url ? (
                <a
                  href={lesson.video.url}
                  download
                  className={buttonVariants({
                    variant: "ghost",
                    size: "sm",
                    className: "shrink-0",
                  })}
                >
                  <Download className="size-4" aria-hidden="true" />
                  Скачать
                </a>
              ) : null}
              {canManage ? (
                <>
                  <Button
                    type="button"
                    variant="destructive"
                    size="sm"
                    className="shrink-0"
                    onClick={() => setIsDeleteDialogOpen(true)}
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                    Удалить
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="shrink-0"
                    onClick={() => setIsVideoDialogOpen(true)}
                  >
                    <UploadCloud className="size-4" aria-hidden="true" />
                    {lesson.video ? "Заменить" : "Загрузить видео"}
                  </Button>
                </>
              ) : null}
            </span>
          ) : null}
        </CardFooter>
      </Card>

      <VideoUploadDialog
        open={isVideoDialogOpen}
        ownerId={lesson.id}
        onOpenChange={setIsVideoDialogOpen}
        onUploadComplete={handleUploadComplete}
        heading={lesson.video ? "Заменить видео" : "Загрузить видео"}
        description={`Урок «${lesson.title}». После загрузки новое видео будет доступно для скачивания.`}
      />

      <Dialog
        open={isDeleteDialogOpen}
        onOpenChange={handleDeleteDialogChange}
      >
        <DialogContent showCloseButton={!deleteLessonMutation.isPending}>
          <DialogHeader>
            <span className="mb-2 flex size-11 items-center justify-center rounded-full bg-destructive/15 text-destructive">
              <Trash2 className="size-5" aria-hidden="true" />
            </span>
            <DialogTitle>Удалить урок?</DialogTitle>
            <DialogDescription>
              Урок «{lesson.title}» будет перемещён в раздел удалённых. Видео
              останется в хранилище.
            </DialogDescription>
          </DialogHeader>

          {deleteLessonMutation.error ? (
            <p
              className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive ring-1 ring-destructive/20"
              role="alert"
            >
              {getErrorMessage(deleteLessonMutation.error)}
            </p>
          ) : null}

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={deleteLessonMutation.isPending}
              onClick={() => handleDeleteDialogChange(false)}
            >
              Отмена
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={deleteLessonMutation.isPending}
              onClick={() => void handleDeleteLesson()}
            >
              {deleteLessonMutation.isPending ? (
                <LoaderCircle
                  className="size-4 animate-spin"
                  aria-hidden="true"
                />
              ) : (
                <Trash2 className="size-4" aria-hidden="true" />
              )}
              {deleteLessonMutation.isPending ? "Удаление…" : "Удалить урок"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
