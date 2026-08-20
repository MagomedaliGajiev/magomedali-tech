"use client";

import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";

import { lessonsApi } from "@/app/entities/lessons/api";
import { APIError, getErrorMessage } from "@/shared/api/errors";

type UseDeleteLessonOptions = {
  onSuccess?: (lessonId: string) => void | Promise<void>;
};

export function useDeleteLesson({ onSuccess }: UseDeleteLessonOptions = {}) {
  return useMutation<string, APIError, string>({
    mutationFn: lessonsApi.deleteLesson,
    onSuccess: async (lessonId) => {
      toast.success("Урок перемещён в удалённые");
      await onSuccess?.(lessonId);
    },
    onError: (error) => {
      console.error("Не удалось удалить урок", {
        error,
        status: error.status,
        type: error.type,
        messages: error.messages,
      });

      toast.error("Не удалось удалить урок", {
        description: getErrorMessage(error),
      });
    },
  });
}
