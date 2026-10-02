/**
 * @brief Represents an error returned from an API request
 * ApiError is thrown when a API request fails, allowing callers
 * to access the HTTP status, error code, and field specific errors
 */
//example use:
//  try{ await register(data) }
//  catch(err) {if (err instanceof ApiError) show(err.message) }
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
 * @brief Represents the possible structure of an API error response.
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
 * @brief Sends a POST request with a JSON body and returns the parsed response
 *
 * if request fails, API error response is parsed and a APIError is thrown
 * containing the error information
 *
 * @tparam TBody Type of request body
 * @tparam TResponse type of the expected successful response
 *
 * @param path API endpoint to send POST to
 * @param body Data to send in request body
 *
 * @returns the parsed successful API response
 * @throws ApiError If the API request fails.
 */
export async function apiPost<TBody, TResponse>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  const res = await fetch(path, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

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
