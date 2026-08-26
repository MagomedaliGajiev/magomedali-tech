"use client";

import { UploadCloud } from "lucide-react";
import { useState, type DragEvent, type KeyboardEvent } from "react";

import { cn } from "@/shared/lib/utils";

type VideoUploadDropzoneProps = {
  disabled?: boolean;
  onClick: () => void;
  onFileSelected: (file: File) => void;
};

export function VideoUploadDropzone({
  disabled = false,
  onClick,
  onFileSelected,
}: VideoUploadDropzoneProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  const handleDragOver = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = disabled ? "none" : "copy";

    if (!disabled) {
      setIsDragOver(true);
    }
  };

  const handleDragLeave = (event: DragEvent<HTMLDivElement>) => {
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) {
      return;
    }

    setIsDragOver(false);
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragOver(false);

    if (disabled) {
      return;
    }

    const file = event.dataTransfer.files.item(0);

    if (file) {
      onFileSelected(file);
    }
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (disabled || (event.key !== "Enter" && event.key !== " ")) {
      return;
    }

    event.preventDefault();
    onClick();
  };

  return (
    <div
      role="button"
      tabIndex={disabled ? -1 : 0}
      aria-disabled={disabled}
      className={cn(
        "group flex min-h-52 flex-col items-center justify-center rounded-xl border border-dashed border-border bg-background/35 px-6 py-8 text-center outline-none transition",
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/30",
        !disabled && "cursor-pointer hover:border-primary/70 hover:bg-primary/5",
        isDragOver && "scale-[1.01] border-primary bg-primary/10 ring-3 ring-primary/15",
        disabled && "cursor-wait opacity-75",
      )}
      onClick={disabled ? undefined : onClick}
      onKeyDown={handleKeyDown}
      onDragEnter={handleDragOver}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <span
        className={cn(
          "mb-4 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary transition-transform",
          isDragOver && "scale-110",
        )}
      >
        <UploadCloud className="size-6" aria-hidden="true" />
      </span>
      <span className="font-medium">
        {isDragOver ? "Отпустите файл" : "Перетащите видео сюда"}
      </span>
      <span className="mt-1 text-sm text-muted-foreground">
        или <span className="font-medium text-primary">выберите на компьютере</span>
      </span>
      <span className="mt-4 text-xs text-muted-foreground">
        MP4, MKV, AVI или MOV · до 5 ГБ
      </span>
    </div>
  );
}
