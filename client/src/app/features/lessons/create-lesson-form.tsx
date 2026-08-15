"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { LoaderCircle, Plus } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { lessonsApi } from "@/app/entities/lessons/api";
import {
  createLessonSchema,
  type CreateLessonRequest,
} from "@/app/entities/lessons/schema";
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
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateLessonRequest>({
    resolver: zodResolver(createLessonSchema),
    defaultValues: EMPTY_FORM,
  });

  const createLessonMutation = useMutation({
    mutationFn: lessonsApi.createLesson,
    onSuccess: async () => {
      toast.success("Урок успешно создан");
      reset();
      await onLessonCreated();
    },
    onError: (error) => {
      toast.error("Не удалось создать урок", {
        description: error.message,
      });
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
    reset();
    createLessonMutation.reset();
    onCancel();
  };

  return (
    <form
      className="flex min-h-0 flex-1 flex-col"
      noValidate
      onSubmit={handleSubmit(onSubmit)}
    >
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
          <FieldLabel htmlFor="create-lesson-video-id">ID видео</FieldLabel>
          <Input
            id="create-lesson-video-id"
            placeholder="00000000-0000-0000-0000-000000000000"
            required
            aria-invalid={Boolean(errors.videoId)}
            aria-describedby={
              errors.videoId
                ? "create-lesson-video-id-description create-lesson-video-id-error"
                : "create-lesson-video-id-description"
            }
            {...register("videoId")}
          />
          <FieldDescription id="create-lesson-video-id-description">
            Видео должно быть заранее загружено в файловый сервис.
          </FieldDescription>
          {errors.videoId ? (
            <FieldError id="create-lesson-video-id-error">
              {errors.videoId.message}
            </FieldError>
          ) : null}
        </Field>

        {createLessonMutation.error ? (
          <FieldError>{createLessonMutation.error.message}</FieldError>
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
    </form>
  );
}
