"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import { videoApi } from "@/app/entities/videos/api";
import type { PartETag } from "@/app/entities/videos/types";
import { getErrorMessage } from "@/shared/api/errors";

const MAX_VIDEO_SIZE = 5_368_709_120;
const ALLOWED_EXTENSIONS = new Set(["mp4", "mkv", "avi", "mov"]);
const CONTENT_TYPE_BY_EXTENSION: Record<string, string> = {
  mp4: "video/mp4",
  mkv: "video/x-matroska",
  avi: "video/x-msvideo",
  mov: "video/quicktime",
};

export type VideoUploadState = "idle" | "uploading" | "completed" | "error";
export type VideoUploadPhase =
  | "preparing"
  | "uploading"
  | "completing"
  | "attaching";

type UseVideoUploadOptions = {
  onUploadComplete: (mediaAssetId: string) => void | Promise<void>;
};

const getFileContentType = (file: File): string => {
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
  onUploadComplete,
}: UseVideoUploadOptions) {
  const abortControllerRef = useRef<AbortController | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadState, setUploadState] = useState<VideoUploadState>("idle");
  const [uploadPhase, setUploadPhase] =
    useState<VideoUploadPhase>("preparing");
  const [progress, setProgress] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [completedMediaAssetId, setCompletedMediaAssetId] = useState<
    string | null
  >(null);

  useEffect(
    () => () => {
      abortControllerRef.current?.abort();
    },
    [],
  );

  const reset = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    setSelectedFile(null);
    setUploadState("idle");
    setUploadPhase("preparing");
    setProgress(0);
    setErrorMessage(null);
    setCompletedMediaAssetId(null);
  }, []);

  const attachUploadedVideo = useCallback(
    async (mediaAssetId: string) => {
      setUploadState("uploading");
      setUploadPhase("attaching");
      setProgress(100);
      setErrorMessage(null);

      try {
        await onUploadComplete(mediaAssetId);
        setUploadState("completed");
      } catch (error: unknown) {
        setUploadState("error");
        setErrorMessage(
          getErrorMessage(
            error,
            "Видео загружено, но его не удалось привязать",
          ),
        );
      }
    },
    [onUploadComplete],
  );

  const uploadFile = useCallback(
    async (file: File) => {
      let contentType: string;

      try {
        contentType = getFileContentType(file);
      } catch (error: unknown) {
        setSelectedFile(file);
        setUploadState("error");
        setErrorMessage(getErrorMessage(error));
        setProgress(0);
        setCompletedMediaAssetId(null);
        return;
      }

      const abortController = new AbortController();
      abortControllerRef.current?.abort();
      abortControllerRef.current = abortController;
      setSelectedFile(file);
      setUploadState("uploading");
      setUploadPhase("preparing");
      setProgress(0);
      setErrorMessage(null);
      setCompletedMediaAssetId(null);

      try {
        const upload = await videoApi.startMultipartUpload(
          {
            fileName: file.name,
            assetType: "video",
            contentType,
            size: file.size,
          },
          abortController.signal,
        );
        const chunks = [...upload.chunkUploadUrls].sort(
          (left, right) => left.partNumber - right.partNumber,
        );
        const expectedChunks = Math.ceil(file.size / upload.chunkSize);

        if (
          !Number.isSafeInteger(upload.chunkSize) ||
          upload.chunkSize <= 0 ||
          chunks.length !== expectedChunks ||
          chunks.some((chunk, index) => chunk.partNumber !== index + 1)
        ) {
          throw new Error("Сервер вернул некорректную схему частей файла");
        }

        setUploadPhase("uploading");
        const partETags: PartETag[] = [];
        let uploadedBytes = 0;

        for (const chunkInfo of chunks) {
          const start = (chunkInfo.partNumber - 1) * upload.chunkSize;
          const end = Math.min(start + upload.chunkSize, file.size);
          const chunk = file.slice(start, end, contentType);
          const eTag = await videoApi.uploadChunk({
            uploadUrl: chunkInfo.uploadUrl,
            chunk,
            contentType,
            signal: abortController.signal,
            onProgress: (chunkLoadedBytes) => {
              const totalUploaded =
                uploadedBytes + Math.min(chunkLoadedBytes, chunk.size);
              setProgress(
                Math.min(99, Math.round((totalUploaded / file.size) * 100)),
              );
            },
          });

          uploadedBytes += chunk.size;
          partETags.push({ partNumber: chunkInfo.partNumber, eTag });
        }

        setUploadPhase("completing");
        setProgress(99);
        await videoApi.completeMultipartUpload(
          {
            mediaAssetId: upload.mediaAssetId,
            uploadId: upload.uploadId,
            partETags,
          },
          abortController.signal,
        );

        setCompletedMediaAssetId(upload.mediaAssetId);
        await attachUploadedVideo(upload.mediaAssetId);
      } catch (error: unknown) {
        if (abortController.signal.aborted) {
          return;
        }

        setUploadState("error");
        setErrorMessage(getErrorMessage(error, "Не удалось загрузить видео"));
      } finally {
        if (abortControllerRef.current === abortController) {
          abortControllerRef.current = null;
        }
      }
    },
    [attachUploadedVideo],
  );

  const retry = useCallback(async () => {
    if (completedMediaAssetId) {
      await attachUploadedVideo(completedMediaAssetId);
      return;
    }

    if (selectedFile) {
      await uploadFile(selectedFile);
    }
  }, [attachUploadedVideo, completedMediaAssetId, selectedFile, uploadFile]);

  return {
    errorMessage,
    isUploading: uploadState === "uploading",
    progress,
    reset,
    retry,
    selectedFile,
    uploadFile,
    uploadPhase,
    uploadState,
  };
}
