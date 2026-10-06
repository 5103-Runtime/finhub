/**
 * @brief Represents an error returned from an API request.
 */
export class ApiError extends Error {
  status: number;
  errorCode?: string;
  fieldErrors?: Record<string, string[]>;

  constructor(
    status: number,
    message: string,
    errorCode?: string,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errorCode = errorCode;
    this.fieldErrors = fieldErrors;
  }
}

interface ErrorBody {
  error?: string;
  errorCode?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function sendJson<TBody, TResponse>(
  path: string,
  method: "GET" | "POST" | "PUT",
  body?: TBody,
): Promise<TResponse> {
  let res: Response;
  try {
    res = await fetch(path, {
      method,
      credentials: "include",
      headers:
        body === undefined ? undefined : { "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(
      0,
      "Could not reach the server. Please try again.",
      "network_error",
    );
  }

  if (!res.ok) {
    const data = (await res.json().catch(() => null)) as ErrorBody | null;
    const firstFieldError = data?.errors
      ? Object.values(data.errors)[0]?.[0]
      : undefined;
    const message =
      data?.error ??
      firstFieldError ??
      data?.detail ??
      data?.title ??
      `Request failed (${res.status})`;

    throw new ApiError(res.status, message, data?.errorCode, data?.errors);
  }

  if (res.status === 204) {
    return undefined as TResponse;
  }

  return (await res.json()) as TResponse;
}

export function apiGet<TResponse>(path: string): Promise<TResponse> {
  return sendJson<undefined, TResponse>(path, "GET");
}

export function apiPost<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "POST", body);
}

export function apiPut<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "PUT", body);
}
