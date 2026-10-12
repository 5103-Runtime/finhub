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

/**
 * @brief Represents the structure of an error response body returned from an API request.
 */
interface ErrorBody {
  error?: string;
  errorCode?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const refreshExcludedPaths = new Set([
  "/api/auth/login",
  "/api/auth/register",
  "/api/auth/refresh",
  "/api/auth/logout",
  "/api/auth/forgot-password",
  "/api/auth/reset-password",
]);

/**
 * Sends an authenticated JSON request and transparently retries once after
 * refreshing the session when the server returns 401 Unauthorized.
 * @param path The API endpoint path to send the request to.
 * @param method The HTTP method to use for the request.
 * @param body The request body to send, when the endpoint accepts a body.
 * @param allowRefresh Whether to attempt a session refresh after a 401 response.
 * @returns A promise that resolves to the parsed response body, or undefined for a 204 response.
 * @throws ApiError When the network request fails or the API returns a non-success response.
 */
async function sendJson<TBody, TResponse>(
  path: string,
  method: "GET" | "POST" | "PUT" | "PATCH",
  body?: TBody,
  allowRefresh = true,
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

  if (res.status === 401 && allowRefresh && !refreshExcludedPaths.has(path)) {
    try {
      await sendJson<undefined, void>(
        "/api/auth/refresh",
        "POST",
        undefined,
        false,
      );
      return sendJson<TBody, TResponse>(path, method, body, false);
    } catch (refreshError) {
      if (!(refreshError instanceof ApiError)) {
        throw refreshError;
      }
    }
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

/**
 * Sends an authenticated GET request to the specified path.
 * @param path The API endpoint path to send the request to.
 * @returns A promise that resolves to the parsed response body.
 * @throws ApiError When the network request fails or the API returns a non-success response.
 */
export function apiGet<TResponse>(path: string): Promise<TResponse> {
  return sendJson<undefined, TResponse>(path, "GET");
}

/**
 * Sends an authenticated POST request to the specified path with a JSON body.
 * @param path The API endpoint path to send the request to.
 * @param body The request body to send with the request.
 * @returns A promise that resolves to the parsed response body, or undefined for a 204 response.
 * @throws ApiError When the network request fails or the API returns a non-success response.
 */
export function apiPost<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "POST", body);
}

/**
 * Sends an authenticated PUT request to the specified path with a JSON body.
 * @param path The API endpoint path to send the request to.
 * @param body The request body to send with the request.
 * @returns A promise that resolves to the parsed response body, or undefined for a 204 response.
 * @throws ApiError When the network request fails or the API returns a non-success response.
 */
export function apiPut<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "PUT", body);
}

/**
 * Sends an authenticated PATCH request to the specified path with a JSON body.
 * @param path The API endpoint path to send the request to.
 * @param body The request body to send with the request.
 * @returns A promise that resolves to the parsed response body, or undefined for a 204 response.
 * @throws ApiError When the network request fails or the API returns a non-success response.
 */
export function apiPatch<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "PATCH", body);
}
