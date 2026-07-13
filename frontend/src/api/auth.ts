import { API_BASE_URL, apiGet, apiPost, ApiError } from "./client";
import type { CurrentUser } from "../types/auth";

/** Full-page redirect to the API's OAuth challenge endpoint — simplest, avoids popup/postMessage. */
export function loginUrl(provider: "google" | "microsoft", returnUrl = "/rules"): string {
  const params = new URLSearchParams({ returnUrl });
  return `${API_BASE_URL}/api/auth/login/${provider}?${params.toString()}`;
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  try {
    return await apiGet<CurrentUser>("/api/account/me");
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      return null;
    }
    throw err;
  }
}

export function logout(): Promise<void> {
  return apiPost<void>("/api/auth/logout");
}
