import { z } from "zod";

const EMPTY_UUID = "00000000-0000-0000-0000-000000000000";

const nonEmptyGuidSchema = (message: string) =>
  z.guid(message).refine((id) => id !== EMPTY_UUID, {
    message: "ID не может быть пустым UUID",
  });

export const createLessonSchema = z.object({
  id: z.union([
    z.literal(""),
    nonEmptyGuidSchema("Не удалось подготовить ID урока"),
  ]),
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
  videoId: z.union([
    z.literal(""),
    nonEmptyGuidSchema("Укажите корректный UUID загруженного видео"),
  ]),
});

export type CreateLessonRequest = z.infer<typeof createLessonSchema>;
