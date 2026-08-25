import {
  request as httpRequest,
  type IncomingHttpHeaders,
  type OutgoingHttpHeaders,
} from "node:http";
import { request as httpsRequest } from "node:https";

import type { NextRequest } from "next/server";

export const runtime = "nodejs";

const STORAGE_INTERNAL_URL =
  process.env.STORAGE_INTERNAL_URL ?? "http://localhost:9000";

const isValidHost = (value: string): boolean =>
  /^(?:[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?|\[[0-9a-f:]+\])(?::\d{1,5})?$/i.test(
    value,
  );

const createUpstreamUrl = (request: NextRequest, path: string[]): URL => {
  if (
    path.length === 0 ||
    path.some((segment) => !segment || segment === "." || segment === "..")
  ) {
    throw new Error("Некорректный путь к объекту хранилища");
  }

  const url = new URL(STORAGE_INTERNAL_URL);
  const basePath = url.pathname.replace(/\/$/, "");
  const objectPath = path.map(encodeURIComponent).join("/");

  url.pathname = `${basePath}/${objectPath}`;
  url.search = request.nextUrl.search;

  return url;
};

type UpstreamResponse = {
  body: Buffer;
  headers: IncomingHttpHeaders;
  statusCode: number;
  statusMessage?: string;
};

type StorageRouteContext = {
  params: Promise<{ path: string[] }>;
};

const STORAGE_REQUEST_HEADERS = [
  "range",
  "if-match",
  "if-none-match",
  "if-modified-since",
  "if-unmodified-since",
] as const;

const STORAGE_RESPONSE_HEADERS = [
  "accept-ranges",
  "cache-control",
  "content-disposition",
  "content-encoding",
  "content-length",
  "content-range",
  "content-type",
  "etag",
  "last-modified",
] as const;

const uploadToStorage = (
  request: NextRequest,
  upstreamUrl: URL,
  headers: OutgoingHttpHeaders,
): Promise<UpstreamResponse> =>
  new Promise((resolve, reject) => {
    const sendRequest =
      upstreamUrl.protocol === "https:" ? httpsRequest : httpRequest;
    const upstreamRequest = sendRequest(
      upstreamUrl,
      { method: "PUT", headers },
      (upstreamResponse) => {
        const chunks: Buffer[] = [];

        upstreamResponse.on("data", (chunk: Buffer | Uint8Array) => {
          chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
        });
        upstreamResponse.on("end", () => {
          resolve({
            body: Buffer.concat(chunks),
            headers: upstreamResponse.headers,
            statusCode: upstreamResponse.statusCode ?? 502,
            statusMessage: upstreamResponse.statusMessage,
          });
        });
        upstreamResponse.on("error", reject);
      },
    );

    const abortUpload = () =>
      upstreamRequest.destroy(new Error("Загрузка отменена"));

    upstreamRequest.on("error", reject);
    upstreamRequest.on("close", () => {
      request.signal.removeEventListener("abort", abortUpload);
    });

    if (request.signal.aborted) {
      abortUpload();
      return;
    }

    request.signal.addEventListener("abort", abortUpload, { once: true });

    if (!request.body) {
      upstreamRequest.end();
      return;
    }

    const uploadBody = async () => {
      const reader = request.body!.getReader();

      try {
        while (true) {
          const { done, value } = await reader.read();

          if (done) {
            upstreamRequest.end();
            break;
          }

          if (!upstreamRequest.write(value)) {
            await new Promise<void>((resume) =>
              upstreamRequest.once("drain", resume),
            );
          }
        }
      } catch (error: unknown) {
        upstreamRequest.destroy(
          error instanceof Error ? error : new Error("Ошибка чтения файла"),
        );
      } finally {
        reader.releaseLock();
      }
    };

    void uploadBody();
  });

export async function PUT(
  request: NextRequest,
  { params }: StorageRouteContext,
) {
  try {
    const { path } = await params;
    const upstreamUrl = createUpstreamUrl(request, path);
    const headers: OutgoingHttpHeaders = {};
    const contentType = request.headers.get("content-type");
    const contentLength = request.headers.get("content-length");
    const signedHost = request.headers.get("x-storage-signed-host");

    if (contentType) {
      headers["content-type"] = contentType;
    }

    if (contentLength) {
      headers["content-length"] = contentLength;
    }

    if (signedHost && isValidHost(signedHost)) {
      headers.host = signedHost;
    }

    const upstreamResponse = await uploadToStorage(
      request,
      upstreamUrl,
      headers,
    );
    const responseHeaders = new Headers();
    const eTag = upstreamResponse.headers.etag;

    if (typeof eTag === "string") {
      responseHeaders.set("etag", eTag);
    }

    return new Response(upstreamResponse.body.toString("utf8"), {
      status: upstreamResponse.statusCode,
      statusText: upstreamResponse.statusMessage,
      headers: responseHeaders,
    });
  } catch (error: unknown) {
    const message =
      error instanceof Error
        ? error.message
        : "Не удалось передать файл в хранилище";

    return Response.json({ error: message }, { status: 502 });
  }
}

const readFromStorage = async (
  request: NextRequest,
  { params }: StorageRouteContext,
  method: "GET" | "HEAD",
): Promise<Response> => {
  try {
    const { path } = await params;
    const upstreamUrl = createUpstreamUrl(request, path);
    const requestHeaders = new Headers();

    for (const headerName of STORAGE_REQUEST_HEADERS) {
      const value = request.headers.get(headerName);

      if (value) {
        requestHeaders.set(headerName, value);
      }
    }

    const upstreamResponse = await fetch(upstreamUrl, {
      // S3 presigned GET URLs include the HTTP method in their signature.
      // Use GET upstream for a browser HEAD request, then discard the body.
      method: "GET",
      headers: requestHeaders,
      cache: "no-store",
      signal: request.signal,
    });
    const responseHeaders = new Headers();

    for (const headerName of STORAGE_RESPONSE_HEADERS) {
      const value = upstreamResponse.headers.get(headerName);

      if (value) {
        responseHeaders.set(headerName, value);
      }
    }

    const responseHasBody =
      method === "GET" &&
      ![204, 205, 304].includes(upstreamResponse.status);

    if (method === "HEAD") {
      await upstreamResponse.body?.cancel();
    }

    return new Response(responseHasBody ? upstreamResponse.body : null, {
      status: upstreamResponse.status,
      statusText: upstreamResponse.statusText,
      headers: responseHeaders,
    });
  } catch (error: unknown) {
    const message =
      error instanceof Error
        ? error.message
        : "Не удалось получить файл из хранилища";

    return Response.json({ error: message }, { status: 502 });
  }
};

export async function GET(
  request: NextRequest,
  context: StorageRouteContext,
) {
  return readFromStorage(request, context, "GET");
}

export async function HEAD(
  request: NextRequest,
  context: StorageRouteContext,
) {
  return readFromStorage(request, context, "HEAD");
}
