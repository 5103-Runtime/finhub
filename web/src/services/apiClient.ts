import { getAccessToken } from "./authStorage";

/**
 * @brief Represents a error returned from a API request
 * ApiError is thrown when a API request fails, allowing callers
 * to access the HTTP status, error code, and field specific errors
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
 * @brief Represents the possible structure of a API error response.
 * API may return different error information depending on the type of failure
 */
interface ErrorBody {
  error?: string;
  errorCode?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/**
 * @brief Sends a JSON request to the specified API endpoint.
 * @param path The API endpoint to call
 * @param method The HTTP method to use
 * @param body The request body
 * @returns A promise resolving to the response data
 */
async function sendJson<TBody, TResponse>(
  path: string,
  method: "POST" | "PUT",
  body: TBody,
): Promise<TResponse> {
  let res: Response;
  try {
    const accessToken = getAccessToken();
    res = await fetch(path, {
      method,
      headers: {
        "Content-Type": "application/json",
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
      body: JSON.stringify(body),
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

  return (await res.json()) as TResponse;
}

/**
 * @brief Sends a POST request to the specified API endpoint with a JSON body.
 * @param path The API endpoint to call
 * @param body The request body
 * @returns The response data
 */
export function apiPost<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "POST", body);
}

/**
 * @brief Sends a PUT request to the specified API endpoint with a JSON body.
 * @param path The API endpoint to call
 * @param body The request body
 * @returns The response data
 */
export function apiPut<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return sendJson<TBody, TResponse>(path, "PUT", body);
}
