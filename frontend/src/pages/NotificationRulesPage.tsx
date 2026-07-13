import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createNotificationRule,
  deleteNotificationRule,
  listNotificationRules,
  updateNotificationRule,
} from "../api/notificationRules";
import { RuleForm } from "../components/RuleForm";
import { RuleList } from "../components/RuleList";
import type { NotificationRule, UpsertNotificationRuleRequest } from "../types/notificationRules";

const RULES_QUERY_KEY = ["notification-rules"];

export function NotificationRulesPage() {
  const queryClient = useQueryClient();
  const [editingRule, setEditingRule] = useState<NotificationRule | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const { data: rules, isLoading } = useQuery({
    queryKey: RULES_QUERY_KEY,
    queryFn: listNotificationRules,
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: RULES_QUERY_KEY });

  const createMutation = useMutation({
    mutationFn: createNotificationRule,
    onSuccess: () => {
      invalidate();
      setIsCreating(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpsertNotificationRuleRequest }) =>
      updateNotificationRule(id, request),
    onSuccess: () => {
      invalidate();
      setEditingRule(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteNotificationRule,
    onSuccess: invalidate,
  });

  const isFormOpen = isCreating || editingRule !== null;
  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>My alerts</h1>
        {!isFormOpen && (
          <button className="primary" onClick={() => setIsCreating(true)}>
            + New alert
          </button>
        )}
      </header>

      {isFormOpen && (
        <RuleForm
          initial={editingRule ?? undefined}
          isSaving={isSaving}
          onCancel={() => {
            setIsCreating(false);
            setEditingRule(null);
          }}
          onSubmit={(request) => {
            if (editingRule) {
              updateMutation.mutate({ id: editingRule.id, request });
            } else {
              createMutation.mutate(request);
            }
          }}
        />
      )}

      {isLoading ? (
        <p className="history-chart-status">Loading alerts…</p>
      ) : (
        <RuleList
          rules={rules ?? []}
          onEdit={(rule) => {
            setIsCreating(false);
            setEditingRule(rule);
          }}
          onDelete={(rule) => {
            if (confirm(`Delete the ${rule.city} alert?`)) {
              deleteMutation.mutate(rule.id);
            }
          }}
        />
      )}
    </div>
  );
}
