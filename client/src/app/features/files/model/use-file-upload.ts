"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import { filesApi } from "@/app/entities/files/api";
import type {
  AssetType,
  OwnerType,
  PartETag,
} from "@/app/entities/files/types";
import { getErrorMessage } from "@/shared/api/errors";

const DEFAULT_MAX_PARALLEL_CHUNKS = 3;

export type FileUploadState =
  | "idle"
  | "uploading"
  | "completed"
  | "failed"
  | "error";

export type FileUploadPhase =
  | "preparing"
  | "uploading"
  | "completing"
  | "finalizing";

type ActiveUpload = {
  mediaAssetId: string;
  uploadId: string;
};

type UseFileUploadOptions = {
  assetType: AssetType;
  ownerType: OwnerType;
  ownerId: string;
  validateFile: (file: File) => string;
  onUploadComplete?: (mediaAssetId: string) => void | Promise<void>;
  maxParallelChunks?: number;
};

const cleanupUpload = async (upload: ActiveUpload) => {
  try {
    await filesApi.deleteMediaAsset(upload.mediaAssetId, upload.uploadId);
  } catch (error: unknown) {
    console.error("Не удалось очистить незавершённую загрузку", error);
  }
};

export function useFileUpload({
  assetType,
  ownerType,
  ownerId,
  validateFile,
  onUploadComplete,
  maxParallelChunks = DEFAULT_MAX_PARALLEL_CHUNKS,
}: UseFileUploadOptions) {
  const abortControllerRef = useRef<AbortController | null>(null);
  const activeUploadRef = useRef<ActiveUpload | null>(null);
  const operationIdRef = useRef(0);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadState, setUploadState] = useState<FileUploadState>("idle");
  const [uploadPhase, setUploadPhase] =
    useState<FileUploadPhase>("preparing");
  const [progress, setProgress] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [completedMediaAssetId, setCompletedMediaAssetId] = useState<
    string | null
  >(null);

  useEffect(
    () => () => {
      operationIdRef.current += 1;
      abortControllerRef.current?.abort();
    },
    [],
  );

  const reset = useCallback(() => {
    operationIdRef.current += 1;
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;

    const activeUpload = activeUploadRef.current;
    activeUploadRef.current = null;
    if (activeUpload) {
      void cleanupUpload(activeUpload);
    }

    setSelectedFile(null);
    setUploadState("idle");
    setUploadPhase("preparing");
    setProgress(0);
    setErrorMessage(null);
    setCompletedMediaAssetId(null);
  }, []);

  const finalizeUpload = useCallback(
    async (mediaAssetId: string, operationId: number) => {
      if (operationIdRef.current !== operationId) {
        return;
      }

      setUploadState("uploading");
      setUploadPhase("finalizing");
      setProgress(100);
      setErrorMessage(null);

      try {
        await onUploadComplete?.(mediaAssetId);

        if (operationIdRef.current !== operationId) {
          return;
        }

        activeUploadRef.current = null;
        setUploadState("completed");
      } catch (error: unknown) {
        if (operationIdRef.current !== operationId) {
          return;
        }

        setUploadState("failed");
        setErrorMessage(
          getErrorMessage(
            error,
            "Файл загружен, но завершить операцию не удалось",
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
        contentType = validateFile(file);
      } catch (error: unknown) {
        operationIdRef.current += 1;
        abortControllerRef.current?.abort();
        setSelectedFile(file);
        setUploadState("error");
        setUploadPhase("preparing");
        setErrorMessage(getErrorMessage(error));
        setProgress(0);
        setCompletedMediaAssetId(null);
        return;
      }

      const previousUpload = activeUploadRef.current;
      if (previousUpload) {
        void cleanupUpload(previousUpload);
      }

      const operationId = operationIdRef.current + 1;
      operationIdRef.current = operationId;
      const abortController = new AbortController();
      abortControllerRef.current?.abort();
      abortControllerRef.current = abortController;
      activeUploadRef.current = null;
      setSelectedFile(file);
      setUploadState("uploading");
      setUploadPhase("preparing");
      setProgress(0);
      setErrorMessage(null);
      setCompletedMediaAssetId(null);

      let startedUpload: ActiveUpload | null = null;

      try {
        const upload = await filesApi.startMultipartUpload(
          {
            fileName: file.name,
            assetType,
            contentType,
            size: file.size,
            ownerType,
            ownerId,
          },
          abortController.signal,
        );
        startedUpload = {
          mediaAssetId: upload.mediaAssetId,
          uploadId: upload.uploadId,
        };
        activeUploadRef.current = startedUpload;

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
        const uploadedBytesByPart = new Map<number, number>();
        let nextChunkIndex = 0;

        const uploadNextChunk = async () => {
          while (nextChunkIndex < chunks.length) {
            const chunkInfo = chunks[nextChunkIndex];
            nextChunkIndex += 1;
            const start = (chunkInfo.partNumber - 1) * upload.chunkSize;
            const end = Math.min(start + upload.chunkSize, file.size);
            const chunk = file.slice(start, end, contentType);
            const eTag = await filesApi.uploadChunk({
              uploadUrl: chunkInfo.uploadUrl,
              chunk,
              contentType,
              signal: abortController.signal,
              onProgress: (loadedBytes) => {
                if (operationIdRef.current !== operationId) {
                  return;
                }

                uploadedBytesByPart.set(
                  chunkInfo.partNumber,
                  Math.min(loadedBytes, chunk.size),
                );
                const totalUploaded = [...uploadedBytesByPart.values()].reduce(
                  (total, value) => total + value,
                  0,
                );
                setProgress(
                  Math.min(99, Math.round((totalUploaded / file.size) * 100)),
                );
              },
            });

            uploadedBytesByPart.set(chunkInfo.partNumber, chunk.size);
            partETags.push({ partNumber: chunkInfo.partNumber, eTag });
          }
        };

        const configuredWorkerCount =
          Number.isSafeInteger(maxParallelChunks) && maxParallelChunks > 0
            ? maxParallelChunks
            : DEFAULT_MAX_PARALLEL_CHUNKS;
        const workerCount = Math.min(configuredWorkerCount, chunks.length);

        try {
          await Promise.all(
            Array.from({ length: workerCount }, () => uploadNextChunk()),
          );
        } catch (error: unknown) {
          abortController.abort(error);
          throw error;
        }

        setUploadPhase("completing");
        setProgress(99);
        await filesApi.completeMultipartUpload(
          {
            mediaAssetId: upload.mediaAssetId,
            uploadId: upload.uploadId,
            partETags,
          },
          abortController.signal,
        );

        setCompletedMediaAssetId(upload.mediaAssetId);
        await finalizeUpload(upload.mediaAssetId, operationId);
      } catch (error: unknown) {
        if (startedUpload && completedMediaAssetId !== startedUpload.mediaAssetId) {
          if (
            activeUploadRef.current?.mediaAssetId === startedUpload.mediaAssetId
          ) {
            activeUploadRef.current = null;
          }
          void cleanupUpload(startedUpload);
        }

        if (operationIdRef.current !== operationId) {
          return;
        }

        setUploadState("failed");
        setErrorMessage(getErrorMessage(error, "Не удалось загрузить файл"));
      } finally {
        if (abortControllerRef.current === abortController) {
          abortControllerRef.current = null;
        }
      }
    },
    [
      assetType,
      completedMediaAssetId,
      finalizeUpload,
      maxParallelChunks,
      ownerId,
      ownerType,
      validateFile,
    ],
  );

  const retry = useCallback(async () => {
    const operationId = operationIdRef.current + 1;
    operationIdRef.current = operationId;

    if (completedMediaAssetId) {
      await finalizeUpload(completedMediaAssetId, operationId);
      return;
    }

    if (selectedFile) {
      await uploadFile(selectedFile);
    }
  }, [completedMediaAssetId, finalizeUpload, selectedFile, uploadFile]);

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
