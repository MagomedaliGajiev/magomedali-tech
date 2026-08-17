"use client";

import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";

import { lessonsApi } from "@/app/entities/lessons/api";
import type { CreateLessonRequest } from "@/app/entities/lessons/schema";
import { APIError, getErrorMessage } from "@/shared/api/errors";

type UseCreateLessonOptions = {
  onSuccess?: (lessonId: string) => void | Promise<void>;
};

export function useCreateLesson({ onSuccess }: UseCreateLessonOptions = {}) {
  return useMutation<string, APIError, CreateLessonRequest>({
    mutationFn: lessonsApi.createLesson,
    onSuccess: async (lessonId) => {
      toast.success("Урок успешно создан");
      await onSuccess?.(lessonId);
    },
    onError: (error) => {
      console.error("Не удалось создать урок", {
        error,
        status: error.status,
        type: error.type,
        messages: error.messages,
      });

      toast.error("Не удалось создать урок", {
        description: getErrorMessage(error),
      });
    },
  });
}
