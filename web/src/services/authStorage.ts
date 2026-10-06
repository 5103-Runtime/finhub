/**
 * Authentication tokens are intentionally not accessible to browser JavaScript.
 * The API owns the session in an HttpOnly cookie.
 */
export function clearAuthSession(): void {
  sessionStorage.removeItem("needs_setup");
}
