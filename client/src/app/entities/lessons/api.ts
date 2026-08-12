import { apiClient } from "@/shared/api/axios-nstance";
import { Lesson } from "./types";

export type CreateLessonRequest = {
  titlle: string;
  description: string;
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

export const lessonsApi = {
  getLessons: async (request: GetLessonRequest): Promise<Lesson[]> => {
    const response = await apiClient.get<Envelope<Lesson[]>>("/lessons", {
      params: request,
    });
    return response.data.result || [];
  },

  createLesson: async (request: CreateLessonRequest) => {
    const resonse = await apiClient.post("/lessons", request);

    return resonse.data;
  },
};
