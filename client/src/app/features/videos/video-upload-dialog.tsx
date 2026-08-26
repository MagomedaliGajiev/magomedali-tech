"use client";

import {
  AlertCircle,
  CheckCircle2,
  FileVideo2,
  LoaderCircle,
  RotateCcw,
} from "lucide-react";
import { useRef, type ChangeEvent } from "react";

import {
  type VideoUploadPhase,
  useVideoUpload,
} from "@/app/features/videos/model/use-video-upload";
import { VideoUploadDropzone } from "@/app/features/videos/video-upload-dropzone";
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

type VideoUploadDialogProps = {
  open: boolean;
  ownerId: string;
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

const getUploadPhaseLabel = (phase: VideoUploadPhase): string => {
  switch (phase) {
    case "preparing":
      return "Подготавливаем загрузку…";
    case "completing":
      return "Собираем части видео…";
    case "finalizing":
      return "Привязываем видео…";
    default:
      return "Загружаем видео…";
  }
};

export function VideoUploadDialog({
  open,
  ownerId,
  onOpenChange,
  onUploadComplete,
  heading = "Загрузка видео",
  description = "Выберите видео — оно будет безопасно загружено в хранилище частями.",
}: VideoUploadDialogProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const {
    errorMessage,
    isUploading,
    progress,
    reset,
    retry,
    selectedFile,
    uploadFile,
    uploadPhase,
    uploadState,
  } = useVideoUpload({ ownerId, onUploadComplete });

  const resetDialog = () => {
    reset();

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
    void retry();
  };

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

        {(uploadState === "error" || uploadState === "failed") &&
        errorMessage ? (
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
                Файл сохранён в хранилище и доступен для скачивания.
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
              {uploadState === "failed" && selectedFile ? (
                <Button type="button" onClick={handleRetry}>
                  <RotateCcw className="size-4" aria-hidden="true" />
                  Повторить
                </Button>
              ) : uploadState === "error" ? (
                <Button type="button" onClick={() => inputRef.current?.click()}>
                  Выбрать другой файл
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
