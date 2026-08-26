import { z } from "zod";

const EMPTY_UUID = "00000000-0000-0000-0000-000000000000";

export const createLessonSchema = z.object({
  id: z
    .string()
    .trim()
    .pipe(z.guid("Не удалось подготовить ID урока"))
    .refine((id) => id !== EMPTY_UUID, {
      message: "ID урока не может быть пустым UUID",
    }),
  title: z
    .string()
    .trim()
    .min(1, "Укажите название урока")
    .max(200, "Название не должно превышать 200 символов"),
  description: z
    .string()
    .trim()
    .min(1, "Укажите описание урока")
    .max(2000, "Описание не должно превышать 2000 символов"),
  videoId: z
    .string()
    .trim()
    .pipe(z.guid("Укажите корректный UUID загруженного видео"))
    .refine((videoId) => videoId !== EMPTY_UUID, {
      message: "ID видео не может быть пустым UUID",
    }),
});

export type CreateLessonRequest = z.infer<typeof createLessonSchema>;
