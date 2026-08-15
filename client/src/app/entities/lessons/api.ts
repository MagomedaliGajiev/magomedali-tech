import { apiClient } from "@/shared/api/axios-nstance";
import axios from "axios";
import type { Lesson, MediaDto } from "./types";
import type { CreateLessonRequest } from "./schema";

export type GetLessonRequest = {
  search?: string;
  page: number;
  pageSize: number;
};

export type Envelope<T = unknown> = {
  result: T | null;
  error: ApiError | null;
  isError: boolean;
  timeGenerated: string;
};

export type ApiError = {
  messages: ErrorMessage[];
  type: ErrorType;
};

export type ErrorMessage = {
  code: string;
  message: string;
  invalidField?: string | null;
};

export type ErrorType =
  | "validation"
  | "not_found"
  | "failure"
  | "conflict"
  | "authentication"
  | "authorization";

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

const getEnvelopeResult = <T>(envelope: Envelope<T>): T => {
  if (envelope.isError || envelope.result === null) {
    const message = envelope.error?.messages
      .map((errorMessage) => errorMessage.message)
      .join("; ");

    throw new Error(message || "Сервер вернул пустой ответ");
  }

  return envelope.result;
};

const getApiErrorMessage = (error: unknown): string => {
  if (!axios.isAxiosError<Envelope>(error)) {
    return error instanceof Error ? error.message : "Неизвестная ошибка";
  }

  const message = error.response?.data.error?.messages
    .map((errorMessage) => errorMessage.message)
    .join("; ");

  return message || error.message;
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
  video: lesson.video ?? undefined,
  createdAt: parseDate(lesson.createdAt),
  updatedAt: parseDate(lesson.updatedAt),
});

export const lessonsApi = {
  getLessons: async (
    request: GetLessonRequest,
    signal?: AbortSignal,
  ): Promise<PaginatedLessons> => {
    const response = await apiClient.get<
      Envelope<PaginationLessonResponse<LessonDto>>
    >("/lessons", {
        params: request,
        signal,
    });

    const result = getEnvelopeResult(response.data);

    return {
      items: result.items.map(mapLesson),
      totalCount: result.totalCount,
      page: result.page,
      pageSize: result.pageSize,
      totalPages: result.totalPages,
    };
  },

  createLesson: async (request: CreateLessonRequest): Promise<string> => {
    try {
      const response = await apiClient.post<Envelope<string>>(
        "/lessons",
        request,
      );

      return getEnvelopeResult(response.data);
    } catch (error) {
      throw new Error(getApiErrorMessage(error));
    }
  },
};
