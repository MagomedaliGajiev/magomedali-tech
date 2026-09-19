"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import {
  CheckCircle2,
  LoaderCircle,
  Plus,
  UploadCloud,
  VideoOff,
} from "lucide-react";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";

import {
  createLessonSchema,
  type CreateLessonRequest,
} from "@/app/entities/lessons/schema";
import { filesApi } from "@/app/entities/files/api";
import { useCreateLesson } from "@/app/features/lessons/model/use-create-lesson";
import { VideoUploadDialog } from "@/app/features/videos/video-upload-dialog";
import { getErrorMessage } from "@/shared/api/errors";
import { Button } from "@/shared/components/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
} from "@/shared/components/ui/field";
import { Input } from "@/shared/components/ui/input";
import { SheetFooter } from "@/shared/components/ui/sheet";

const EMPTY_FORM: CreateLessonRequest = {
  id: "",
  title: "",
  description: "",
  videoId: "",
};

type CreateLessonFormProps = {
  onCancel: () => void;
  onLessonCreated: () => void | Promise<void>;
};

export function CreateLessonForm({
  onCancel,
  onLessonCreated,
}: CreateLessonFormProps) {
  const {
    register,
    control,
    getValues,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<CreateLessonRequest>({
    resolver: zodResolver(createLessonSchema),
    defaultValues: EMPTY_FORM,
  });
  const [isVideoUploadOpen, setIsVideoUploadOpen] = useState(false);
  const lessonId = useWatch({ control, name: "id" });
  const videoId = useWatch({ control, name: "videoId" });

  const createLessonMutation = useCreateLesson({
    onSuccess: async () => {
      reset();
      await onLessonCreated();
    },
  });

  const onSubmit = async (request: CreateLessonRequest) => {
    try {
      await createLessonMutation.mutateAsync(request);
    } catch {
      // Ошибка уже обработана в onError мутации и показана пользователю.
    }
  };

  const handleCancel = () => {
    const currentVideoId = getValues("videoId");
    if (currentVideoId) {
      void filesApi.deleteMediaAsset(currentVideoId).catch((error: unknown) => {
        console.error("Не удалось удалить видео отменённого урока", error);
      });
    }

    reset();
    createLessonMutation.reset();
    setIsVideoUploadOpen(false);
    onCancel();
  };

  const handleVideoUploaded = (mediaAssetId: string) => {
    const previousVideoId = getValues("videoId");
    setValue("videoId", mediaAssetId, {
      shouldDirty: true,
      shouldTouch: true,
      shouldValidate: true,
    });

    if (previousVideoId && previousVideoId !== mediaAssetId) {
      void filesApi.deleteMediaAsset(previousVideoId).catch((error: unknown) => {
        console.error("Не удалось удалить заменённое видео", error);
      });
    }
  };

  const handleOpenVideoUpload = () => {
    if (!getValues("id")) {
      setValue("id", crypto.randomUUID(), {
        shouldDirty: false,
        shouldTouch: false,
        shouldValidate: false,
      });
    }

    setIsVideoUploadOpen(true);
  };

  const handleRemoveVideo = () => {
    const currentVideoId = getValues("videoId");
    if (!currentVideoId) {
      return;
    }

    setValue("videoId", "", {
      shouldDirty: true,
      shouldTouch: true,
      shouldValidate: true,
    });
    void filesApi.deleteMediaAsset(currentVideoId).catch((error: unknown) => {
      console.error("Не удалось удалить непривязанное видео", error);
    });
  };

  return (
    <form
      className="flex min-h-0 flex-1 flex-col"
      noValidate
      onSubmit={handleSubmit(onSubmit)}
    >
      <input type="hidden" {...register("id")} />
      <div className="flex-1 space-y-5 overflow-y-auto px-6 py-2">
        <Field data-invalid={Boolean(errors.title)}>
          <FieldLabel htmlFor="create-lesson-title">Название</FieldLabel>
          <Input
            id="create-lesson-title"
            maxLength={200}
            required
            autoFocus
            aria-invalid={Boolean(errors.title)}
            aria-describedby={
              errors.title ? "create-lesson-title-error" : undefined
            }
            {...register("title")}
          />
          {errors.title ? (
            <FieldError id="create-lesson-title-error">
              {errors.title.message}
            </FieldError>
          ) : null}
        </Field>

        <Field data-invalid={Boolean(errors.description)}>
          <FieldLabel htmlFor="create-lesson-description">
            Описание
          </FieldLabel>
          <textarea
            id="create-lesson-description"
            className="min-h-28 w-full resize-y rounded-lg border border-input bg-transparent px-3 py-2 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
            maxLength={2000}
            required
            aria-invalid={Boolean(errors.description)}
            aria-describedby={
              errors.description
                ? "create-lesson-description-error"
                : undefined
            }
            {...register("description")}
          />
          {errors.description ? (
            <FieldError id="create-lesson-description-error">
              {errors.description.message}
            </FieldError>
          ) : null}
        </Field>

        <Field data-invalid={Boolean(errors.videoId)}>
          <FieldLabel>Видео урока</FieldLabel>
          <input type="hidden" {...register("videoId")} />
          <div className="flex items-center gap-3 rounded-lg border border-input bg-background/35 p-3">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              {videoId ? (
                <CheckCircle2
                  className="size-5 text-emerald-400"
                  aria-hidden="true"
                />
              ) : (
                <UploadCloud className="size-5" aria-hidden="true" />
              )}
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-medium">
                {videoId ? "Видео загружено" : "Видео ещё не выбрано"}
              </span>
              {videoId ? (
                <span className="block truncate font-mono text-xs text-muted-foreground">
                  {videoId}
                </span>
              ) : null}
            </span>
            <span className="flex items-center gap-2">
              {videoId ? (
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  disabled={isSubmitting}
                  onClick={handleRemoveVideo}
                >
                  <VideoOff className="size-4" aria-hidden="true" />
                  Убрать
                </Button>
              ) : null}
              <Button
                type="button"
                size="sm"
                variant={videoId ? "outline" : "default"}
                disabled={isSubmitting}
                onClick={handleOpenVideoUpload}
              >
                {videoId ? "Заменить" : "Загрузить"}
              </Button>
            </span>
          </div>
          <FieldDescription id="create-lesson-video-id-description">
            Необязательно. Файл загружается частями и обрабатывается после
            создания урока.
          </FieldDescription>
          {errors.videoId ? (
            <FieldError id="create-lesson-video-id-error">
              {errors.videoId.message}
            </FieldError>
          ) : null}
        </Field>

        {createLessonMutation.error ? (
          <FieldError role="alert">
            {getErrorMessage(createLessonMutation.error)}
          </FieldError>
        ) : null}
      </div>

      <SheetFooter className="border-t border-white/10 px-6 py-5">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? (
            <LoaderCircle className="size-4 animate-spin" />
          ) : (
            <Plus className="size-4" />
          )}
          {isSubmitting ? "Создание…" : "Создать урок"}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={isSubmitting}
          onClick={handleCancel}
        >
          Отмена
        </Button>
      </SheetFooter>

      <VideoUploadDialog
        open={isVideoUploadOpen}
        ownerId={lessonId}
        onOpenChange={setIsVideoUploadOpen}
        onUploadComplete={handleVideoUploaded}
        heading="Видео нового урока"
      />
    </form>
  );
}
