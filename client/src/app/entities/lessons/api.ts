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

export const lessonsApi = {
  getLessons: async (request: GetLessonRequest): Promise<Lesson[]> => {
    const response = await apiClient.get<Lesson[]>("/lessons", {
      params: request,
    });
    return response.data;
  },

  createLesson: async (request: CreateLessonRequest) => {
    const resonse = await apiClient.post("/lessons", request);

    return resonse.data;
  },
};
