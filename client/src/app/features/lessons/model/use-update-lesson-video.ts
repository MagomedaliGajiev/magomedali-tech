"use client";

import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";

import { lessonsApi } from "@/app/entities/lessons/api";
import { APIError, getErrorMessage } from "@/shared/api/errors";

type UpdateLessonVideoVariables = {
  lessonId: string;
  videoId: string | null;
};

type UseUpdateLessonVideoOptions = {
  onSuccess?: (
    lessonId: string,
    videoId: string | null,
  ) => void | Promise<void>;
};

export function useUpdateLessonVideo({
  onSuccess,
}: UseUpdateLessonVideoOptions = {}) {
  return useMutation<string, APIError, UpdateLessonVideoVariables>({
    mutationFn: ({ lessonId, videoId }) =>
      lessonsApi.updateLessonVideo(lessonId, videoId),
    onSuccess: async (lessonId, variables) => {
      toast.success(
        variables.videoId ? "Видео урока обновлено" : "Видео удалено из урока",
      );
      await onSuccess?.(lessonId, variables.videoId);
    },
    onError: (error) => {
      console.error("Не удалось обновить видео урока", {
        error,
        status: error.status,
        type: error.type,
        messages: error.messages,
      });

      toast.error("Не удалось обновить видео урока", {
        description: getErrorMessage(error),
      });
    },
  });
}
