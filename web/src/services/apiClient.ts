//sends JSON to API and throws a ApiError whenever a request fails

//error object , client throws when the API sends a failure
//example use:
//  try{ await signUp(data) }
//  catch(err) {if (err instanceof ApiError) show(err.message) }
export class ApiError extends Error{

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

//typescript interface Error response
interface ErrorBody {
  error?: string;
  errorCode?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

//send POST request with JSON body, returns parsed JSON response
export async function apiPost<TBody, TResponse>(
    path: string,
    body: TBody,

): Promise<TResponse> {
    const res = await fetch(path, {
        method: "POST",
        headers: { "Content-Type": "application/json"},
        body: JSON.stringify(body),
    });

  const data = (await res.json().catch(() => null)) as ErrorBody | null;

  if(!res.ok) {
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
  return data as unknown as TResponse;
}