"use client";

import { useMutation } from "@tanstack/react-query";
import { LoaderCircle, Plus } from "lucide-react";
import { type FormEvent, useState } from "react";
import { toast } from "sonner";

import {
  lessonsApi,
  type CreateLessonRequest,
} from "@/app/entities/lessons/api";
import { Button } from "@/shared/components/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
} from "@/shared/components/ui/field";
import { Input } from "@/shared/components/ui/input";
import { SheetFooter } from "@/shared/components/ui/sheet";

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

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
  const [form, setForm] = useState<CreateLessonRequest>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);

  const createLessonMutation = useMutation({
    mutationFn: (request: CreateLessonRequest) =>
      lessonsApi.createLesson(request),
    onSuccess: async () => {
      toast.success("Урок успешно создан");
      await onLessonCreated();
    },
    onError: (error) => {
      toast.error("Не удалось создать урок", {
        description: error.message,
      });
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const request = {
      title: form.title.trim(),
      description: form.description.trim(),
      videoId: form.videoId.trim(),
    };

    if (!request.title || !request.description) {
      setFormError("Заполните название и описание урока");
      return;
    }

    if (!UUID_PATTERN.test(request.videoId)) {
      setFormError("Укажите корректный UUID загруженного видео");
      return;
    }

    setFormError(null);
    createLessonMutation.mutate(request);
  };

  return (
    <form className="flex min-h-0 flex-1 flex-col" onSubmit={handleSubmit}>
      <div className="flex-1 space-y-5 overflow-y-auto px-6 py-2">
        <Field>
          <FieldLabel htmlFor="create-lesson-title">Название</FieldLabel>
          <Input
            id="create-lesson-title"
            value={form.title}
            maxLength={200}
            required
            autoFocus
            onChange={(event) =>
              setForm((currentForm) => ({
                ...currentForm,
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
            value={form.description}
            maxLength={2000}
            required
            onChange={(event) =>
              setForm((currentForm) => ({
                ...currentForm,
                description: event.target.value,
              }))
            }
          />
        </Field>

        <Field data-invalid={Boolean(formError)}>
          <FieldLabel htmlFor="create-lesson-video-id">ID видео</FieldLabel>
          <Input
            id="create-lesson-video-id"
            value={form.videoId}
            placeholder="00000000-0000-0000-0000-000000000000"
            required
            aria-invalid={Boolean(formError)}
            onChange={(event) =>
              setForm((currentForm) => ({
                ...currentForm,
                videoId: event.target.value,
              }))
            }
          />
          <FieldDescription>
            Видео должно быть заранее загружено в файловый сервис.
          </FieldDescription>
        </Field>

        {formError ? <FieldError>{formError}</FieldError> : null}

        {createLessonMutation.error ? (
          <FieldError>{createLessonMutation.error.message}</FieldError>
        ) : null}
      </div>

      <SheetFooter className="border-t border-white/10 px-6 py-5">
        <Button type="submit" disabled={createLessonMutation.isPending}>
          {createLessonMutation.isPending ? (
            <LoaderCircle className="size-4 animate-spin" />
          ) : (
            <Plus className="size-4" />
          )}
          {createLessonMutation.isPending ? "Создание…" : "Создать урок"}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={createLessonMutation.isPending}
          onClick={onCancel}
        >
          Отмена
        </Button>
      </SheetFooter>
    </form>
  );
}
