import { apiDelete, apiGet, apiPost, apiPut } from "./client";
import type { NotificationRule, UpsertNotificationRuleRequest } from "../types/notificationRules";

export function listNotificationRules(): Promise<NotificationRule[]> {
  return apiGet<NotificationRule[]>("/api/notification-rules");
}

export function createNotificationRule(request: UpsertNotificationRuleRequest): Promise<NotificationRule> {
  return apiPost<NotificationRule>("/api/notification-rules", request);
}

export function updateNotificationRule(
  id: string,
  request: UpsertNotificationRuleRequest,
): Promise<NotificationRule> {
  return apiPut<NotificationRule>(`/api/notification-rules/${id}`, request);
}

export function deleteNotificationRule(id: string): Promise<void> {
  return apiDelete<void>(`/api/notification-rules/${id}`);
}
