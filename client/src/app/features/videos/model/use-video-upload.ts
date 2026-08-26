"use client";

import { AssetType, OwnerType } from "@/app/entities/files/types";
import {
  type FileUploadPhase,
  type FileUploadState,
  useFileUpload,
} from "@/app/features/files/model/use-file-upload";

const MAX_VIDEO_SIZE = 5_368_709_120;
const ALLOWED_EXTENSIONS = new Set(["mp4", "mkv", "avi", "mov"]);
const CONTENT_TYPE_BY_EXTENSION: Record<string, string> = {
  mp4: "video/mp4",
  mkv: "video/x-matroska",
  avi: "video/x-msvideo",
  mov: "video/quicktime",
};

export type VideoUploadState = FileUploadState;
export type VideoUploadPhase = FileUploadPhase;

type UseVideoUploadOptions = {
  ownerId: string;
  onUploadComplete: (mediaAssetId: string) => void | Promise<void>;
};

const validateVideoFile = (file: File): string => {
  const extension = file.name.split(".").pop()?.toLowerCase() ?? "";

  if (!ALLOWED_EXTENSIONS.has(extension)) {
    throw new Error("Поддерживаются только видео MP4, MKV, AVI и MOV");
  }

  if (file.size === 0) {
    throw new Error("Выбранный файл пуст");
  }

  if (file.size > MAX_VIDEO_SIZE) {
    throw new Error("Размер видео не должен превышать 5 ГБ");
  }

  if (file.type && !file.type.startsWith("video/")) {
    throw new Error("Выбранный файл не является видео");
  }

  return file.type || CONTENT_TYPE_BY_EXTENSION[extension];
};

export function useVideoUpload({
  ownerId,
  onUploadComplete,
}: UseVideoUploadOptions) {
  return useFileUpload({
    assetType: AssetType.VIDEO,
    ownerType: OwnerType.LESSON,
    ownerId,
    validateFile: validateVideoFile,
    onUploadComplete,
  });
}
