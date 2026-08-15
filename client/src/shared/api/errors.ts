import { isAxiosError } from "axios";

export const API_ERROR_TYPE = {
  VALIDATION: "VALIDATION",
  NOT_FOUND: "NOT_FOUND",
  FAILURE: "FAILURE",
  CONFLICT: "CONFLICT",
  AUTHENTICATION: "AUTHENTICATION",
  AUTHORIZATION: "AUTHORIZATION",
} as const;

export type APIErrorType =
  (typeof API_ERROR_TYPE)[keyof typeof API_ERROR_TYPE];

export type APIErrorMessage = {
  code: string;
  message: string;
  invalidField?: string | null;
};

export type APIErrorDetails = {
  messages: APIErrorMessage[];
  type: APIErrorType;
};

export type APIEnvelope<T = unknown> = {
  result: T | null;
  error: APIErrorDetails | null;
  isError: boolean;
  timeGenerated: string;
};

type APIErrorEnvelope = APIEnvelope<unknown> & {
  error: APIErrorDetails;
  isError: true;
};

const DEFAULT_ERROR_MESSAGE = "Произошла непредвиденная ошибка";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const isAPIErrorType = (value: unknown): value is APIErrorType =>
  typeof value === "string" &&
  Object.values(API_ERROR_TYPE).some((type) => type === value);

const isAPIErrorMessage = (value: unknown): value is APIErrorMessage =>
  isRecord(value) &&
  typeof value.code === "string" &&
  typeof value.message === "string" &&
  (value.invalidField === undefined ||
    value.invalidField === null ||
    typeof value.invalidField === "string");

const isAPIErrorDetails = (value: unknown): value is APIErrorDetails =>
  isRecord(value) &&
  Array.isArray(value.messages) &&
  value.messages.every(isAPIErrorMessage) &&
  isAPIErrorType(value.type);

export const isAPIErrorEnvelope = (
  value: unknown,
): value is APIErrorEnvelope =>
  isRecord(value) &&
  value.isError === true &&
  isAPIErrorDetails(value.error);

const joinMessages = (messages: APIErrorMessage[]): string =>
  messages
    .map(({ message }) => message.trim())
    .filter(Boolean)
    .join("; ");

export class APIError extends Error {
  readonly messages: APIErrorMessage[];
  readonly type: APIErrorType | null;
  readonly status: number | undefined;

  constructor({
    messages,
    type = null,
    status,
    cause,
    fallbackMessage = DEFAULT_ERROR_MESSAGE,
  }: {
    messages?: APIErrorMessage[];
    type?: APIErrorType | null;
    status?: number;
    cause?: unknown;
    fallbackMessage?: string;
  }) {
    const normalizedMessages = messages ?? [];

    super(joinMessages(normalizedMessages) || fallbackMessage, { cause });

    this.name = "APIError";
    this.messages = normalizedMessages;
    this.type = type;
    this.status = status;
  }

  toJSON() {
    return {
      name: this.name,
      message: this.message,
      messages: this.messages,
      type: this.type,
      status: this.status,
    };
  }
}

export const toAPIError = (
  error: unknown,
  fallbackMessage = DEFAULT_ERROR_MESSAGE,
): APIError => {
  if (error instanceof APIError) {
    return error;
  }

  if (isAxiosError<unknown>(error)) {
    const responseData = error.response?.data;

    if (isAPIErrorEnvelope(responseData)) {
      return new APIError({
        messages: responseData.error.messages,
        type: responseData.error.type,
        status: error.response?.status,
        cause: error,
        fallbackMessage,
      });
    }

    return new APIError({
      status: error.response?.status,
      cause: error,
      fallbackMessage: error.message || fallbackMessage,
    });
  }

  if (error instanceof Error) {
    return new APIError({
      cause: error,
      fallbackMessage: error.message || fallbackMessage,
    });
  }

  return new APIError({ cause: error, fallbackMessage });
};

export const unwrapAPIEnvelope = <T>(
  envelope: APIEnvelope<T>,
  emptyResultMessage = "Сервер вернул пустой ответ",
): T => {
  if (envelope.isError) {
    if (envelope.error) {
      throw new APIError({
        messages: envelope.error.messages,
        type: envelope.error.type,
      });
    }

    throw new APIError({ fallbackMessage: DEFAULT_ERROR_MESSAGE });
  }

  if (envelope.result === null) {
    throw new APIError({ fallbackMessage: emptyResultMessage });
  }

  return envelope.result;
};

export const getErrorMessage = (
  error: unknown,
  fallbackMessage = DEFAULT_ERROR_MESSAGE,
): string => toAPIError(error, fallbackMessage).message;
