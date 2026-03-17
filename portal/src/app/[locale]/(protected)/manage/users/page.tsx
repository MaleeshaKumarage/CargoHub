"use client";

import { useAuth } from "@/context/AuthContext";
import { useRouter } from "@/i18n/navigation";
import { useEffect, useState } from "react";
import {
  adminGetCompanies,
  adminGetUsers,
  adminPatchUser,
  type AdminCompany,
  type AdminUser,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

const ROLES = ["SuperAdmin", "Admin", "User"] as const;

export default function ManageUsersPage() {
  const { token } = useAuth();
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterBusinessId, setFilterBusinessId] = useState<string>("");
  const [updating, setUpdating] = useState<string | null>(null);
  const [pendingRole, setPendingRole] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    setError(null);
    setLoading(true);
    Promise.all([
      adminGetCompanies(token),
      adminGetUsers(token, filterBusinessId || undefined),
    ])
      .then(([companiesList, usersList]) => {
        if (!cancelled) {
          setCompanies(companiesList);
          setUsers(usersList);
        }
      })
      .catch((e) => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [token, filterBusinessId]);

  const refetchUsers = () => {
    if (!token) return;
    adminGetUsers(token, filterBusinessId || undefined)
      .then(setUsers)
      .catch(() => {});
  };

  const setRoleSelection = (userId: string, role: string) => {
    setPendingRole((prev) => ({ ...prev, [userId]: role }));
  };

  const handleSaveRole = async (userId: string, role: string) => {
    if (!token) return;
    setUpdating(userId);
    try {
      await adminPatchUser(token, userId, { role });
      setPendingRole((prev) => {
        const next = { ...prev };
        delete next[userId];
        return next;
      });
      refetchUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setUpdating(null);
    }
  };

  const handleActiveChange = async (userId: string, isActive: boolean) => {
    if (!token) return;
    setUpdating(userId);
    try {
      await adminPatchUser(token, userId, { isActive });
      refetchUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setUpdating(null);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Users</h1>
        <p className="text-muted-foreground mt-1">
          View users by company, change roles, and activate or deactivate accounts.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Users</CardTitle>
          <CardDescription>
            Filter by company (optional). Change role with the dropdown, then click Save for that row. Use the checkbox to activate or deactivate (saves immediately).
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center gap-4">
            <div className="space-y-2">
              <Label htmlFor="company-filter">Company (Business ID)</Label>
              <select
                id="company-filter"
                className="flex h-9 w-[200px] rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                value={filterBusinessId}
                onChange={(e) => setFilterBusinessId(e.target.value)}
              >
                <option value="">All companies</option>
                {companies.map((c) => (
                  <option key={c.id} value={c.businessId ?? ""}>
                    {c.businessId || c.companyId || c.id}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}

          {loading ? (
            <p className="text-muted-foreground">Loading…</p>
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left font-medium">Email</th>
                    <th className="p-3 text-left font-medium">Display name</th>
                    <th className="p-3 text-left font-medium">Company (Business ID)</th>
                    <th className="p-3 text-left font-medium">Role</th>
                    <th className="p-3 text-left font-medium">Active</th>
                    <th className="p-3 text-left font-medium w-[80px]">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => {
                    const currentRole = u.roles[0] ?? "User";
                    const selectedRole = pendingRole[u.userId] ?? currentRole;
                    const roleChanged = selectedRole !== currentRole;
                    return (
                      <tr key={u.userId} className="border-b last:border-0">
                        <td className="p-3">{u.email}</td>
                        <td className="p-3">{u.displayName}</td>
                        <td className="p-3">{u.businessId ?? "—"}</td>
                        <td className="p-3">
                          <select
                            className="rounded border border-input bg-background px-2 py-1 text-sm"
                            value={selectedRole}
                            disabled={updating === u.userId}
                            onChange={(e) => setRoleSelection(u.userId, e.target.value)}
                          >
                            {ROLES.map((r) => (
                              <option key={r} value={r}>
                                {r}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="p-3">
                          <label className="flex items-center gap-2">
                            <input
                              type="checkbox"
                              checked={u.isActive}
                              disabled={updating === u.userId}
                              onChange={(e) => handleActiveChange(u.userId, e.target.checked)}
                              className="h-4 w-4 rounded border-input"
                            />
                            {u.isActive ? "Active" : "Inactive"}
                          </label>
                        </td>
                        <td className="p-3">
                          {roleChanged && (
                            <Button
                              size="sm"
                              variant="default"
                              disabled={updating === u.userId}
                              onClick={() => handleSaveRole(u.userId, selectedRole)}
                            >
                              {updating === u.userId ? "Saving…" : "Save"}
                            </Button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              {users.length === 0 && (
                <p className="p-4 text-muted-foreground">No users found.</p>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
