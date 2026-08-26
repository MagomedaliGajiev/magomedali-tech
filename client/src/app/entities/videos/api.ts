import axios from "axios";

import { apiClient } from "@/shared/api/axios-nstance";
import {
  type APIEnvelope,
  ensureAPIEnvelopeSuccess,
  toAPIError,
  unwrapAPIEnvelope,
} from "@/shared/api/errors";
import type {
  CompleteMultipartUploadRequest,
  StartMultipartUploadRequest,
  StartMultipartUploadResponse,
} from "./types";
import { resolveBrowserStorageTarget } from "./storage-endpoint";

export const videoApi = {
  startMultipartUpload: async (
    request: StartMultipartUploadRequest,
    signal?: AbortSignal,
  ): Promise<StartMultipartUploadResponse> => {
    try {
      const response = await apiClient.post<
        APIEnvelope<StartMultipartUploadResponse>
      >("/files/multipart-upload", request, { signal });

      return unwrapAPIEnvelope(response.data);
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось начать загрузку видео");
    }
  },

  uploadChunk: async ({
    uploadUrl,
    chunk,
    contentType,
    signal,
    onProgress,
  }: {
    uploadUrl: string;
    chunk: Blob;
    contentType: string;
    signal?: AbortSignal;
    onProgress?: (loadedBytes: number) => void;
  }): Promise<string> => {
    try {
      const target = resolveBrowserStorageTarget(uploadUrl);
      const response = await axios.put(target.url, chunk, {
        headers: {
          "Content-Type": contentType,
          ...(target.signedHost
            ? { "X-Storage-Signed-Host": target.signedHost }
            : {}),
        },
        signal,
        onUploadProgress: (event) => onProgress?.(event.loaded),
      });
      const eTag = response.headers.etag;

      if (typeof eTag !== "string" || eTag.trim().length === 0) {
        throw new Error(
          "Хранилище не вернуло ETag. Проверьте CORS и ExposeHeaders для ETag.",
        );
      }

      return eTag.trim().replace(/^W\//, "").replace(/^"|"$/g, "");
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось загрузить часть видео");
    }
  },

  completeMultipartUpload: async (
    request: CompleteMultipartUploadRequest,
    signal?: AbortSignal,
  ): Promise<void> => {
    try {
      const response = await apiClient.post<APIEnvelope<null>>(
        "/files/complete-upload",
        request,
        { signal },
      );

      ensureAPIEnvelopeSuccess(response.data);
    } catch (error: unknown) {
      throw toAPIError(error, "Не удалось завершить загрузку видео");
    }
  },
};
