"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useState } from "react";
import {
  adminGetCompanies,
  adminCreateCompany,
  adminPatchCompany,
  type AdminCompany,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

function parseOptionalInt(s: string): number | undefined {
  const t = s.trim();
  if (t === "") return undefined;
  const n = Number(t);
  return Number.isFinite(n) ? Math.floor(n) : undefined;
}

export default function ManageCompaniesPage() {
  const { token } = useAuth();
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [businessId, setBusinessId] = useState("");
  const [maxUsers, setMaxUsers] = useState("");
  const [maxAdmins, setMaxAdmins] = useState("");
  const [initialAdminEmail, setInitialAdminEmail] = useState("");
  const [resendingId, setResendingId] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    setError(null);
    adminGetCompanies(token)
      .then((list) => {
        if (!cancelled) setCompanies(list);
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
  }, [token]);

  const refetchCompanies = () => {
    if (!token) return;
    adminGetCompanies(token).then(setCompanies).catch(() => {});
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    const companyName = name.trim();
    const bid = businessId.trim();
    if (!companyName) {
      setCreateError("Name is required.");
      return;
    }
    if (!bid) {
      setCreateError("Business ID is required (used for registration and admin invite).");
      return;
    }
    setCreating(true);
    setCreateError(null);
    try {
      await adminCreateCompany(token, {
        name: companyName,
        businessId: bid,
        maxUserAccounts: parseOptionalInt(maxUsers),
        maxAdminAccounts: parseOptionalInt(maxAdmins),
        initialAdminEmail: initialAdminEmail.trim() || undefined,
      });
      setName("");
      setBusinessId("");
      setMaxUsers("");
      setMaxAdmins("");
      setInitialAdminEmail("");
      refetchCompanies();
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : "Create failed");
    } finally {
      setCreating(false);
    }
  };

  const handleResendInvite = async (companyId: string) => {
    if (!token) return;
    setResendingId(companyId);
    try {
      await adminPatchCompany(token, companyId, { resendAdminInvite: true });
      refetchCompanies();
    } catch {
      // ignore; could add toast
    } finally {
      setResendingId(null);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Companies</h1>
        <p className="text-muted-foreground mt-1">
          Create companies with limits and optional initial admin email. An invite is sent automatically when the company
          has no administrator yet (fallback address is <code className="text-xs">businessId@example.com</code> if you
          leave admin email blank—configure domain in API <code className="text-xs">Portal:CompanyAdminFallbackEmailDomain</code>
          ).
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create company</CardTitle>
          <CardDescription>
            Uses Super Admin API. Sends an admin invite when there are no admins yet for this Business ID.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleCreate} className="flex flex-col gap-4">
            <div className="flex flex-wrap items-end gap-4">
              <div className="space-y-2">
                <Label htmlFor="company-name">Name *</Label>
                <Input
                  id="company-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Acme Oy"
                  className="w-[220px]"
                  disabled={creating}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="business-id">Business ID *</Label>
                <Input
                  id="business-id"
                  value={businessId}
                  onChange={(e) => setBusinessId(e.target.value)}
                  placeholder="e.g. 1234567-8"
                  className="w-[180px]"
                  disabled={creating}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="max-users">Max users</Label>
                <Input
                  id="max-users"
                  type="number"
                  min={1}
                  value={maxUsers}
                  onChange={(e) => setMaxUsers(e.target.value)}
                  placeholder="optional"
                  className="w-[120px]"
                  disabled={creating}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="max-admins">Max admins</Label>
                <Input
                  id="max-admins"
                  type="number"
                  min={1}
                  value={maxAdmins}
                  onChange={(e) => setMaxAdmins(e.target.value)}
                  placeholder="optional"
                  className="w-[120px]"
                  disabled={creating}
                />
              </div>
            </div>
            <div className="space-y-2 max-w-md">
              <Label htmlFor="initial-admin">Initial admin email (optional)</Label>
              <Input
                id="initial-admin"
                type="email"
                value={initialAdminEmail}
                onChange={(e) => setInitialAdminEmail(e.target.value)}
                placeholder="admin@customer.com — or leave blank for fallback invite"
                disabled={creating}
              />
            </div>
            <Button type="submit" disabled={creating}>
              {creating ? "Creating…" : "Create company"}
            </Button>
          </form>
          {createError && (
            <p className="mt-3 text-sm text-destructive" role="alert">
              {createError}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Companies</CardTitle>
          <CardDescription>Counts reflect active users and Admin role members for each Business ID.</CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <p className="text-sm text-destructive mb-4" role="alert">
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
                    <th className="p-3 text-left font-medium">Name</th>
                    <th className="p-3 text-left font-medium">Business ID</th>
                    <th className="p-3 text-left font-medium">Users / max</th>
                    <th className="p-3 text-left font-medium">Admins / max</th>
                    <th className="p-3 text-left font-medium">Company ID</th>
                    <th className="p-3 text-left font-medium">Invite</th>
                  </tr>
                </thead>
                <tbody>
                  {companies.map((c) => (
                    <tr key={c.id} className="border-b last:border-0">
                      <td className="p-3">{c.name ?? "—"}</td>
                      <td className="p-3">{c.businessId ?? "—"}</td>
                      <td className="p-3">
                        {c.activeUserCount ?? "—"}
                        {c.maxUserAccounts != null ? ` / ${c.maxUserAccounts}` : ""}
                      </td>
                      <td className="p-3">
                        {c.adminCount ?? "—"}
                        {c.maxAdminAccounts != null ? ` / ${c.maxAdminAccounts}` : ""}
                      </td>
                      <td className="p-3 font-mono text-xs">{c.companyId}</td>
                      <td className="p-3">
                        {(c.adminCount ?? 0) === 0 ? (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={resendingId === c.id}
                            onClick={() => handleResendInvite(c.id)}
                          >
                            {resendingId === c.id ? "Sending…" : "Resend admin invite"}
                          </Button>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {companies.length === 0 && (
                <p className="p-4 text-muted-foreground">No companies yet. Create one above.</p>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
