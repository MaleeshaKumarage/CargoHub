"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useState } from "react";
import {
  adminGetCompanies,
  adminCreateCompany,
  adminPatchCompany,
  adminSendTestEmail,
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
  const [resendingId, setResendingId] = useState<string | null>(null);
  const [inviteActionError, setInviteActionError] = useState<string | null>(null);
  const [testEmailTo, setTestEmailTo] = useState("");
  const [testEmailSending, setTestEmailSending] = useState(false);
  const [testEmailMessage, setTestEmailMessage] = useState<string | null>(null);
  const [testEmailIsError, setTestEmailIsError] = useState(false);

  const adminEmailSlotCount = useMemo(() => {
    const n = parseOptionalInt(maxAdmins);
    if (n == null || n < 1) return 1;
    return Math.min(32, n);
  }, [maxAdmins]);

  const [adminEmails, setAdminEmails] = useState<string[]>([""]);

  useEffect(() => {
    setAdminEmails((prev) => {
      if (prev.length === adminEmailSlotCount) return prev;
      const next = prev.slice(0, adminEmailSlotCount);
      while (next.length < adminEmailSlotCount) next.push("");
      return next;
    });
  }, [adminEmailSlotCount]);

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

  const setAdminEmailAt = (index: number, value: string) => {
    setAdminEmails((prev) => {
      const next = [...prev];
      next[index] = value;
      return next;
    });
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
    const filledEmails = adminEmails.map((x) => x.trim()).filter(Boolean);
    const maxA = parseOptionalInt(maxAdmins);
    if (maxA != null && filledEmails.length > maxA) {
      setCreateError(`Enter at most ${maxA} admin email address(es) to match max admins.`);
      return;
    }
    setCreating(true);
    setCreateError(null);
    try {
      await adminCreateCompany(token, {
        name: companyName,
        businessId: bid,
        maxUserAccounts: parseOptionalInt(maxUsers),
        maxAdminAccounts: maxA ?? null,
        initialAdminEmails: filledEmails.length > 0 ? filledEmails : undefined,
      });
      setName("");
      setBusinessId("");
      setMaxUsers("");
      setMaxAdmins("");
      setAdminEmails([""]);
      refetchCompanies();
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : "Create failed");
    } finally {
      setCreating(false);
    }
  };

  const handleResendInvite = async (companyId: string) => {
    if (!token) return;
    setInviteActionError(null);
    setResendingId(companyId);
    try {
      await adminPatchCompany(token, companyId, { resendAdminInvite: true });
      refetchCompanies();
    } catch (e) {
      setInviteActionError(e instanceof Error ? e.message : "Resend failed");
    } finally {
      setResendingId(null);
    }
  };

  const handleTestEmail = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    const to = testEmailTo.trim();
    if (!to) {
      setTestEmailIsError(true);
      setTestEmailMessage("Enter an email address.");
      return;
    }
    setTestEmailSending(true);
    setTestEmailMessage(null);
    setTestEmailIsError(false);
    try {
      const r = await adminSendTestEmail(token, to);
      setTestEmailIsError(false);
      setTestEmailMessage(r.message ?? "Sent. Check the inbox (and spam).");
    } catch (e) {
      setTestEmailIsError(true);
      setTestEmailMessage(e instanceof Error ? e.message : "Failed to send test email");
    } finally {
      setTestEmailSending(false);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Companies</h1>
        <p className="text-muted-foreground mt-1">
          Create companies with limits and admin invite emails. One invite per filled email is sent when the company has
          no administrator yet (if all email fields are empty, a single fallback invite uses{" "}
          <code className="text-xs">businessId@domain</code> — configure{" "}
          <code className="text-xs">Portal:CompanyAdminFallbackEmailDomain</code> on the API).
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create company</CardTitle>
          <CardDescription>
            Uses Super Admin API. Set max admins to N to enter up to N admin emails; each receives its own invite when
            there are no admins yet for this Business ID.
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
                  placeholder="optional (1 email row if empty)"
                  className="w-[200px]"
                  disabled={creating}
                />
              </div>
            </div>
            <div className="space-y-3 max-w-lg">
              <Label>Initial admin email(s) (optional)</Label>
              <p className="text-xs text-muted-foreground">
                Number of fields matches max admins (or one row if max admins is not set). Leave all blank to use the
                fallback address only.
              </p>
              <div className="flex flex-col gap-2">
                {adminEmails.map((em, i) => (
                  <Input
                    key={i}
                    type="email"
                    value={em}
                    onChange={(e) => setAdminEmailAt(i, e.target.value)}
                    placeholder={`Admin email ${i + 1}`}
                    disabled={creating}
                    autoComplete="off"
                  />
                ))}
              </div>
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
          {inviteActionError && (
            <p className="text-sm text-destructive mb-4" role="alert">
              {inviteActionError}
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
                  {companies.map((c) => {
                    const hasAdmin = (c.adminCount ?? 0) > 0;
                    return (
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
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={hasAdmin || resendingId === c.id}
                            title={
                              hasAdmin
                                ? "Company already has an administrator; resend is not available."
                                : "Resend invite to the same address(es) configured for this company."
                            }
                            onClick={() => handleResendInvite(c.id)}
                          >
                            {resendingId === c.id ? "Sending…" : "Resend admin invite"}
                          </Button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              {companies.length === 0 && (
                <p className="p-4 text-muted-foreground">No companies yet. Create one above.</p>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Verify email (SMTP)</CardTitle>
          <CardDescription>
            Sends a short test message through the API&apos;s configured SMTP. Requires <code className="text-xs">Smtp:Host</code>{" "}
            and <code className="text-xs">Smtp:FromAddress</code> (see appsettings or environment variables).
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleTestEmail} className="flex flex-wrap items-end gap-3">
            <div className="space-y-2">
              <Label htmlFor="test-email-to">Recipient</Label>
              <Input
                id="test-email-to"
                type="email"
                value={testEmailTo}
                onChange={(e) => setTestEmailTo(e.target.value)}
                placeholder="you@example.com"
                className="w-[280px]"
                disabled={testEmailSending}
              />
            </div>
            <Button type="submit" variant="secondary" disabled={testEmailSending}>
              {testEmailSending ? "Sending…" : "Send test email"}
            </Button>
          </form>
          {testEmailMessage && (
            <p
              className={`mt-3 text-sm ${testEmailIsError ? "text-destructive" : "text-muted-foreground"}`}
              role={testEmailIsError ? "alert" : "status"}
            >
              {testEmailMessage}
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
