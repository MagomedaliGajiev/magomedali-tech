"use client";

import {
  AlertCircle,
  CheckCircle2,
  FileVideo2,
  LoaderCircle,
  RotateCcw,
} from "lucide-react";
import { useEffect, useRef, useState, type ChangeEvent } from "react";

import { videoApi } from "@/app/entities/videos/api";
import type { PartETag } from "@/app/entities/videos/types";
import { VideoUploadDropzone } from "@/app/features/videos/video-upload-dropzone";
import { getErrorMessage } from "@/shared/api/errors";
import { Button } from "@/shared/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/components/ui/dialog";

const MAX_VIDEO_SIZE = 5_368_709_120;
const ALLOWED_EXTENSIONS = new Set(["mp4", "mkv", "avi", "mov"]);
const CONTENT_TYPE_BY_EXTENSION: Record<string, string> = {
  mp4: "video/mp4",
  mkv: "video/x-matroska",
  avi: "video/x-msvideo",
  mov: "video/quicktime",
};

type UploadState = "idle" | "uploading" | "completed" | "error";

type VideoUploadDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUploadComplete: (mediaAssetId: string) => void | Promise<void>;
  heading?: string;
  description?: string;
};

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) {
    return `${bytes} Б`;
  }

  const units = ["КБ", "МБ", "ГБ"];
  let value = bytes / 1024;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value.toLocaleString("ru-RU", { maximumFractionDigits: 1 })} ${units[unitIndex]}`;
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

const getUploadPhaseLabel = (phase: string): string => {
  switch (phase) {
    case "preparing":
      return "Подготавливаем загрузку…";
    case "completing":
      return "Собираем части видео…";
    case "attaching":
      return "Привязываем видео…";
    default:
      return "Загружаем видео…";
  }
};

export function VideoUploadDialog({
  open,
  onOpenChange,
  onUploadComplete,
  heading = "Загрузка видео",
  description = "Выберите видео — оно будет безопасно загружено в хранилище частями.",
}: VideoUploadDialogProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadState, setUploadState] = useState<UploadState>("idle");
  const [uploadPhase, setUploadPhase] = useState("preparing");
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

  const resetDialog = () => {
    abortControllerRef.current = null;
    setSelectedFile(null);
    setUploadState("idle");
    setUploadPhase("preparing");
    setProgress(0);
    setErrorMessage(null);
    setCompletedMediaAssetId(null);

    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen && uploadState === "uploading") {
      return;
    }

    if (!nextOpen) {
      resetDialog();
    }

    onOpenChange(nextOpen);
  };

  const attachUploadedVideo = async (mediaAssetId: string) => {
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
        getErrorMessage(error, "Видео загружено, но его не удалось привязать"),
      );
    }
  };

  const uploadFile = async (file: File) => {
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
  };

  const handleFileSelected = (file: File) => {
    if (uploadState === "uploading") {
      return;
    }

    void uploadFile(file);
  };

  const handleInputChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.item(0);
    event.currentTarget.value = "";

    if (file) {
      handleFileSelected(file);
    }
  };

  const handleRetry = () => {
    if (completedMediaAssetId) {
      void attachUploadedVideo(completedMediaAssetId);
      return;
    }

    if (selectedFile) {
      void uploadFile(selectedFile);
    }
  };

  const isUploading = uploadState === "uploading";

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!isUploading}>
        <DialogHeader>
          <DialogTitle>{heading}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <input
          ref={inputRef}
          className="hidden"
          type="file"
          accept="video/mp4,video/x-matroska,video/x-msvideo,video/quicktime,.mp4,.mkv,.avi,.mov"
          tabIndex={-1}
          aria-hidden="true"
          onChange={handleInputChange}
        />

        {uploadState !== "completed" ? (
          <VideoUploadDropzone
            disabled={isUploading}
            onClick={() => inputRef.current?.click()}
            onFileSelected={handleFileSelected}
          />
        ) : null}

        {selectedFile ? (
          <div className="flex items-center gap-3 rounded-lg bg-muted/60 p-3">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-background text-muted-foreground">
              <FileVideo2 className="size-5" aria-hidden="true" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-medium">
                {selectedFile.name}
              </span>
              <span className="text-xs text-muted-foreground">
                {formatFileSize(selectedFile.size)}
              </span>
            </span>
          </div>
        ) : null}

        {isUploading ? (
          <div className="space-y-2" aria-live="polite">
            <div className="flex items-center justify-between text-sm">
              <span className="inline-flex items-center gap-2 text-muted-foreground">
                <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
                {getUploadPhaseLabel(uploadPhase)}
              </span>
              <span className="font-medium tabular-nums">{progress}%</span>
            </div>
            <div
              className="h-2 overflow-hidden rounded-full bg-muted"
              role="progressbar"
              aria-label="Прогресс загрузки видео"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={progress}
            >
              <div
                className="h-full rounded-full bg-primary transition-[width] duration-200"
                style={{ width: `${progress}%` }}
              />
            </div>
            <p className="text-xs text-muted-foreground">
              Не закрывайте окно до завершения загрузки.
            </p>
          </div>
        ) : null}

        {uploadState === "error" && errorMessage ? (
          <div
            className="flex items-start gap-2 rounded-lg bg-destructive/10 p-3 text-sm text-destructive ring-1 ring-destructive/20"
            role="alert"
          >
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            <span>{errorMessage}</span>
          </div>
        ) : null}

        {uploadState === "completed" ? (
          <div
            className="flex items-start gap-3 rounded-lg bg-emerald-500/10 p-4 text-emerald-300 ring-1 ring-emerald-400/20"
            role="status"
          >
            <CheckCircle2 className="mt-0.5 size-5 shrink-0" aria-hidden="true" />
            <div>
              <p className="font-medium">Видео загружено</p>
              <p className="mt-1 text-sm text-emerald-200/75">
                Файл передан в хранилище и поставлен в очередь на обработку.
              </p>
            </div>
          </div>
        ) : null}

        <DialogFooter>
          {isUploading ? (
            <Button type="button" disabled>
              <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
              Загрузка…
            </Button>
          ) : uploadState === "completed" ? (
            <DialogClose render={<Button type="button" />}>
              Готово
            </DialogClose>
          ) : (
            <>
              <DialogClose render={<Button type="button" variant="outline" />}>
                Отмена
              </DialogClose>
              {uploadState === "error" && selectedFile ? (
                <Button type="button" onClick={handleRetry}>
                  <RotateCcw className="size-4" aria-hidden="true" />
                  Повторить
                </Button>
              ) : (
                <Button type="button" onClick={() => inputRef.current?.click()}>
                  Выбрать видео
                </Button>
              )}
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
