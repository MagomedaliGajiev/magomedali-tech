export type Lesson = {
  id: string;
  title: string;
  description: string;
  video?: MediaDto;
  createdAt: Date;
  updatedAt: Date;
};

export type MediaDto = {
  id: string;
  url: string;
  status: MediaStatus;
};

export const MediaStatus = {
  UPLOADING: "uploading",
  UPLOADED: "uploaded",
  READY: "ready",
  FAILED: "failed",
  DELETED: "deleted",
} as const;

export type MediaStatus = (typeof MediaStatus)[keyof typeof MediaStatus];
