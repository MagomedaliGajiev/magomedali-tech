import { apiClient } from "@/shared/api/axios-nstance";
import type { Lesson, MediaDto } from "./types";

export type CreateLessonRequest = {
  title: string;
  description: string;
  videoId: string;
};

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

type PaginationLessonResponse = {
  lessons: LessonDto[];
  totalCount: number;
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
  ): Promise<Lesson[]> => {
    const response = await apiClient.get<Envelope<PaginationLessonResponse>>(
      "/lessons",
      {
        params: request,
        signal,
      },
    );

    return getEnvelopeResult(response.data).lessons.map(mapLesson);
  },

  createLesson: async (request: CreateLessonRequest): Promise<string> => {
    const response = await apiClient.post<Envelope<string>>("/lessons", request);

    return getEnvelopeResult(response.data);
  },
};
