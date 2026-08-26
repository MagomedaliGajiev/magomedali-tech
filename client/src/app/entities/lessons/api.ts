import { getBrowserStorageUrl } from "@/app/entities/videos/storage-endpoint";
import { apiClient } from "@/shared/api/axios-nstance";
import {
  type APIEnvelope,
  toAPIError,
  unwrapAPIEnvelope,
} from "@/shared/api/errors";
import type { CreateLessonRequest } from "./schema";
import type { Lesson, MediaDto } from "./types";

export type GetLessonRequest = {
  search?: string;
  isDeleted: boolean;
  page: number;
  pageSize: number;
};

type LessonDto = Omit<Lesson, "createdAt" | "updatedAt" | "video"> & {
  video: MediaDto | null;
  createdAt: string;
  updatedAt: string;
};

type PaginationLessonResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export type PaginatedLessons = {
  items: Lesson[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

const parseDate = (value: string): Date => {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    throw new Error(`Сервер вернул некорректную дату: ${value}`);
  }

  return date;
};

const mapLesson = (lesson: LessonDto): Lesson => ({
  ...lesson,
  video: lesson.video
    ? {
        ...lesson.video,
        url: lesson.video.url
          ? getBrowserStorageUrl(lesson.video.url)
          : null,
      }
    : undefined,
  createdAt: parseDate(lesson.createdAt),
  updatedAt: parseDate(lesson.updatedAt),
});

export const lessonsApi = {
  getLessons: async (
    request: GetLessonRequest,
    signal?: AbortSignal,
  ): Promise<PaginatedLessons> => {
    try {
      const response = await apiClient.get<
        APIEnvelope<PaginationLessonResponse<LessonDto>>
      >("/lessons", {
        params: request,
        signal,
      });

      const result = unwrapAPIEnvelope(response.data);

      return {
        items: result.items.map(mapLesson),
        totalCount: result.totalCount,
        page: result.page,
        pageSize: result.pageSize,
        totalPages: result.totalPages,
      };
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось загрузить уроки");
    }
  },

  createLesson: async (request: CreateLessonRequest): Promise<string> => {
    try {
      const response = await apiClient.post<APIEnvelope<string>>(
        "/lessons",
        request,
      );

      return unwrapAPIEnvelope(response.data);
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось создать урок");
    }
  },

  deleteLesson: async (lessonId: string): Promise<string> => {
    try {
      const response = await apiClient.delete<APIEnvelope<string>>(
        `/lessons/${lessonId}`,
      );

      return unwrapAPIEnvelope(response.data);
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось удалить урок");
    }
  },

  updateLessonVideo: async (
    lessonId: string,
    videoId: string,
  ): Promise<string> => {
    try {
      const response = await apiClient.patch<APIEnvelope<string>>(
        `/lessons/${lessonId}/video`,
        { videoId },
      );

      return unwrapAPIEnvelope(response.data);
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось привязать видео к уроку");
    }
  },
};
