import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getCurrentUser, logout as logoutRequest } from "../api/auth";

export const CURRENT_USER_QUERY_KEY = ["current-user"];

export function useAuth() {
  const queryClient = useQueryClient();

  const { data: user, isLoading } = useQuery({
    queryKey: CURRENT_USER_QUERY_KEY,
    queryFn: getCurrentUser,
    staleTime: 60_000,
  });

  const logout = async () => {
    await logoutRequest();
    queryClient.setQueryData(CURRENT_USER_QUERY_KEY, null);
  };

  return { user: user ?? null, isLoading, isAuthenticated: !!user, logout };
}
