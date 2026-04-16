"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import {
  adminGetCompanies,
  adminCreateCompany,
  adminGetUsers,
  adminGetSubscriptionPlans,
  adminPatchCompany,
  AdminCompanyLimitReductionRequiredError,
  DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID,
  type AdminCompany,
  type AdminPatchCompanyBody,
  type AdminSubscriptionPlanSummary,
  type AdminUser,
  type LimitReductionConflictDetails,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Dialog as DialogPrimitive } from "radix-ui";
import { cn } from "@/lib/utils";

function parseOptionalInt(s: string): number | undefined {
  const t = s.trim();
  if (t === "") return undefined;
  const n = Number(t);
  return Number.isFinite(n) ? Math.floor(n) : undefined;
}

function inviteEmailsKey(emails: string[]): string {
  return emails
    .map((e) => e.trim().toLowerCase())
    .filter(Boolean)
    .sort()
    .join("|");
}

function companyInviteKey(c: AdminCompany): string {
  const list = c.initialAdminInviteEmails?.length
    ? c.initialAdminInviteEmails
    : c.initialAdminInviteEmail
      ? [c.initialAdminInviteEmail]
      : [];
  return inviteEmailsKey(list);
}

function isCompanyAdmin(u: AdminUser): boolean {
  return Array.isArray(u.roles) && u.roles.includes("Admin");
}

function isSuperAdminUser(u: AdminUser): boolean {
  return Array.isArray(u.roles) && u.roles.includes("SuperAdmin");
}

type PendingCompanyLimits = {
  maxUserAccounts?: number | null;
  maxAdminAccounts?: number | null;
};

export default function ManageCompaniesPage() {
  const { token } = useAuth();
  const tMc = useTranslations("manageCompanies");
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
  const [togglingCompanyId, setTogglingCompanyId] = useState<string | null>(null);

  const [editCompany, setEditCompany] = useState<AdminCompany | null>(null);
  const [editMaxUsers, setEditMaxUsers] = useState("");
  const [editMaxAdmins, setEditMaxAdmins] = useState("");
  const [editSubscriptionPlanId, setEditSubscriptionPlanId] = useState("");
  /** Value from API when the edit dialog opened (`null` = company had no plan yet). */
  const [editSubscriptionFromServer, setEditSubscriptionFromServer] = useState<string | null>(null);
  const [savingCompanyId, setSavingCompanyId] = useState<string | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [editInviteEmails, setEditInviteEmails] = useState<string[]>([""]);
  const [editResendInvite, setEditResendInvite] = useState(false);
  const [subscriptionPlans, setSubscriptionPlans] = useState<AdminSubscriptionPlanSummary[]>([]);
  const [createSubscriptionPlanId, setCreateSubscriptionPlanId] = useState("");

  const [reductionFlow, setReductionFlow] = useState<{
    company: AdminCompany;
    details: LimitReductionConflictDetails;
    pending: PendingCompanyLimits;
  } | null>(null);
  const [reductionUsers, setReductionUsers] = useState<AdminUser[]>([]);
  const [reductionUsersLoading, setReductionUsersLoading] = useState(false);
  const [selectedDemote, setSelectedDemote] = useState<Set<string>>(() => new Set());
  const [selectedDeactivate, setSelectedDeactivate] = useState<Set<string>>(() => new Set());
  const [reductionError, setReductionError] = useState<string | null>(null);
  const [applyingReduction, setApplyingReduction] = useState(false);

  const adminEmailSlotCount = useMemo(() => {
    const n = parseOptionalInt(maxAdmins);
    if (n == null || n < 1) return 1;
    return Math.min(32, n);
  }, [maxAdmins]);

  const editInviteEmailSlotCount = useMemo(() => {
    if (!editCompany) return 1;
    const n = parseOptionalInt(editMaxAdmins);
    const fallback = editCompany.maxAdminAccounts;
    const val = n ?? fallback;
    if (val == null || val < 1) return 1;
    return Math.min(32, val);
  }, [editMaxAdmins, editCompany]);

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
    if (!editCompany) return;
    setEditInviteEmails((prev) => {
      if (prev.length === editInviteEmailSlotCount) return prev;
      const next = prev.slice(0, editInviteEmailSlotCount);
      while (next.length < editInviteEmailSlotCount) next.push("");
      return next;
    });
  }, [editCompany, editInviteEmailSlotCount]);

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

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    adminGetSubscriptionPlans(token)
      .then((list) => {
        if (!cancelled) setSubscriptionPlans(list);
      })
      .catch(() => {
        if (!cancelled) setSubscriptionPlans([]);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  const planDisplayName = (planId: string | null | undefined) => {
    if (!planId) return tMc("subscriptionUnknown");
    const p = subscriptionPlans.find((x) => x.id === planId);
    if (!p) return tMc("subscriptionUnknown");
    return p.name + (p.isActive ? "" : tMc("subscriptionInactiveSuffix"));
  };

  useEffect(() => {
    if (!token || !reductionFlow) {
      setReductionUsers([]);
      return;
    }
    let cancelled = false;
    setReductionUsersLoading(true);
    setReductionError(null);
    adminGetUsers(token, reductionFlow.details.businessId)
      .then((list) => {
        if (!cancelled) setReductionUsers(list.filter((u) => !isSuperAdminUser(u)));
      })
      .catch(() => {
        if (!cancelled) setReductionError("Failed to load users for this company.");
      })
      .finally(() => {
        if (!cancelled) setReductionUsersLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [token, reductionFlow]);

  useEffect(() => {
    if (!reductionFlow) {
      setSelectedDemote(new Set());
      setSelectedDeactivate(new Set());
    }
  }, [reductionFlow]);

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

  const setEditInviteEmailAt = (index: number, value: string) => {
    setEditInviteEmails((prev) => {
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
        subscriptionPlanId: createSubscriptionPlanId.trim() || undefined,
      });
      setName("");
      setBusinessId("");
      setMaxUsers("");
      setMaxAdmins("");
      setCreateSubscriptionPlanId("");
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

  const handleToggleCompanyActive = async (company: AdminCompany, nextActive: boolean) => {
    if (!token) return;
    setInviteActionError(null);
    setTogglingCompanyId(company.id);
    try {
      await adminPatchCompany(token, company.id, { isActive: nextActive });
      setCompanies((prev) =>
        prev.map((row) => (row.id === company.id ? { ...row, isActive: nextActive } : row))
      );
    } catch (e) {
      setInviteActionError(e instanceof Error ? e.message : "Status update failed");
    } finally {
      setTogglingCompanyId(null);
    }
  };

  const openEditCompany = (c: AdminCompany) => {
    setEditError(null);
    setEditCompany(c);
    setEditMaxUsers(c.maxUserAccounts != null ? String(c.maxUserAccounts) : "");
    setEditMaxAdmins(c.maxAdminAccounts != null ? String(c.maxAdminAccounts) : "");
    const raw = c.subscriptionPlanId?.trim() ?? "";
    setEditSubscriptionFromServer(raw.length > 0 ? raw : null);
    setEditSubscriptionPlanId(raw.length > 0 ? raw : DEFAULT_TRIAL_SUBSCRIPTION_PLAN_ID);
    setEditResendInvite(false);
    const slotCount = Math.min(32, Math.max(1, c.maxAdminAccounts ?? 1));
    const base =
      c.initialAdminInviteEmails?.length && c.initialAdminInviteEmails.length > 0
        ? [...c.initialAdminInviteEmails]
        : c.initialAdminInviteEmail
          ? [c.initialAdminInviteEmail]
          : [];
    const padded = [...base];
    while (padded.length < slotCount) padded.push("");
    setEditInviteEmails(padded.slice(0, slotCount));
  };

  const handleSaveCompany = async () => {
    if (!token || !editCompany) return;
    const body: AdminPatchCompanyBody = {};
    if (editMaxUsers.trim() !== "") {
      const p = parseOptionalInt(editMaxUsers);
      if (p === undefined) {
        setEditError(tMc("validationMaxUsersInvalid"));
        return;
      }
      body.maxUserAccounts = p;
    }
    if (editMaxAdmins.trim() !== "") {
      const p = parseOptionalInt(editMaxAdmins);
      if (p === undefined) {
        setEditError(tMc("validationMaxAdminsInvalid"));
        return;
      }
      body.maxAdminAccounts = p;
    }
    const hasNoAdmin = (editCompany.adminCount ?? 0) === 0;
    if (hasNoAdmin) {
      const filledInvite = editInviteEmails.map((x) => x.trim()).filter(Boolean);
      const maxA = parseOptionalInt(editMaxAdmins) ?? editCompany.maxAdminAccounts;
      if (maxA != null && filledInvite.length > maxA) {
        setEditError(tMc("inviteTooManyEmails"));
        return;
      }
      if (inviteEmailsKey(filledInvite) !== companyInviteKey(editCompany)) {
        body.initialAdminEmails = filledInvite;
      }
      if (editResendInvite) {
        body.resendAdminInvite = true;
      }
    }
    const desiredSub = editSubscriptionPlanId.trim();
    if (!desiredSub) {
      setEditError(tMc("subscriptionSelectRequired"));
      return;
    }
    if (
      desiredSub &&
      !(editSubscriptionFromServer !== null && editSubscriptionFromServer === desiredSub)
    ) {
      body.subscriptionPlanId = desiredSub;
    }
    if (Object.keys(body).length === 0) {
      setEditError(tMc("validationNoChanges"));
      return;
    }
    setSavingCompanyId(editCompany.id);
    setEditError(null);
    try {
      await adminPatchCompany(token, editCompany.id, body);
      if (body.subscriptionPlanId) setEditSubscriptionFromServer(body.subscriptionPlanId);
      setEditCompany(null);
      refetchCompanies();
    } catch (e) {
      if (e instanceof AdminCompanyLimitReductionRequiredError) {
        setEditError(null);
        setReductionFlow({
          company: editCompany,
          details: e.details,
          pending: {
            maxUserAccounts: body.maxUserAccounts,
            maxAdminAccounts: body.maxAdminAccounts,
          },
        });
        return;
      }
      setEditError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setSavingCompanyId(null);
    }
  };

  const toggleDemote = (userId: string) => {
    setSelectedDemote((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const toggleDeactivate = (userId: string) => {
    setSelectedDeactivate((prev) => {
      const next = new Set(prev);
      if (next.has(userId)) next.delete(userId);
      else next.add(userId);
      return next;
    });
  };

  const handleApplyReduction = async () => {
    if (!token || !reductionFlow) return;
    const { company, details, pending } = reductionFlow;
    const minD = details.minimumAdminsToDemote;
    const minU = details.minimumUsersToDeactivate;
    if (minD > 0 && selectedDemote.size < minD) {
      setReductionError(`Select at least ${minD} administrator(s) to demote to a normal user.`);
      return;
    }
    if (minU > 0 && selectedDeactivate.size < minU) {
      setReductionError(`Select at least ${minU} active user account(s) to deactivate.`);
      return;
    }
    setApplyingReduction(true);
    setReductionError(null);
    const body: AdminPatchCompanyBody = {};
    if (pending.maxUserAccounts !== undefined) body.maxUserAccounts = pending.maxUserAccounts;
    if (pending.maxAdminAccounts !== undefined) body.maxAdminAccounts = pending.maxAdminAccounts;
    const demote = [...selectedDemote];
    const deact = [...selectedDeactivate];
    if (demote.length > 0) body.demoteAdminUserIds = demote;
    if (deact.length > 0) body.deactivateUserIds = deact;
    try {
      await adminPatchCompany(token, company.id, body);
      setReductionFlow(null);
      setEditCompany(null);
      refetchCompanies();
    } catch (e) {
      setReductionError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setApplyingReduction(false);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Companies</h1>
        <p className="text-muted-foreground mt-1">
          Create companies, assign plans, and send admin invitations. One invite per filled email is sent when the company
          has no administrator yet (if all email fields are empty, a single fallback invite uses{" "}
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
              <div className="space-y-2 min-w-[240px]">
                <Label htmlFor="create-subscription">{tMc("subscriptionLabel")}</Label>
                <select
                  id="create-subscription"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={createSubscriptionPlanId}
                  onChange={(e) => setCreateSubscriptionPlanId(e.target.value)}
                  disabled={creating}
                >
                  <option value="">{tMc("subscriptionDefault")}</option>
                  {subscriptionPlans.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} ({p.kind})
                      {p.isActive ? "" : tMc("subscriptionInactiveSuffix")}
                    </option>
                  ))}
                </select>
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
                    <th className="p-3 text-left font-medium">{tMc("activeColumn")}</th>
                    <th className="p-3 text-left font-medium">Users / max</th>
                    <th className="p-3 text-left font-medium">Admins / max</th>
                    <th className="p-3 text-left font-medium">{tMc("subscriptionColumn")}</th>
                    <th className="p-3 text-left font-medium">{tMc("companyActionsColumn")}</th>
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
                          <div className="flex items-center gap-2">
                            <Switch
                              checked={c.isActive}
                              disabled={togglingCompanyId === c.id}
                              onCheckedChange={(checked) => void handleToggleCompanyActive(c, checked)}
                              aria-label={tMc("activeColumn")}
                            />
                            <span className="text-xs text-muted-foreground">
                              {c.isActive ? tMc("activeStateOn") : tMc("activeStateOff")}
                            </span>
                          </div>
                        </td>
                        <td className="p-3">
                          {c.activeUserCount ?? "—"}
                          {c.maxUserAccounts != null ? ` / ${c.maxUserAccounts}` : ""}
                        </td>
                        <td className="p-3">
                          {c.adminCount ?? "—"}
                          {c.maxAdminAccounts != null ? ` / ${c.maxAdminAccounts}` : ""}
                        </td>
                        <td className="p-3 max-w-[200px] truncate" title={planDisplayName(c.subscriptionPlanId)}>
                          {planDisplayName(c.subscriptionPlanId)}
                        </td>
                        <td className="p-3">
                          <Button
                            type="button"
                            variant="secondary"
                            size="sm"
                            onClick={() => openEditCompany(c)}
                          >
                            {tMc("editButton")}
                          </Button>
                        </td>
                        <td className="p-3">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={hasAdmin || resendingId === c.id}
                            title={
                              hasAdmin
                                ? "Company already has an administrator; resend is not available."
                                : "Open Edit to change invite emails or use Resend after saving. Quick resend uses the saved list."
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

      <DialogPrimitive.Root
        open={!!editCompany}
        onOpenChange={(open) => {
          if (!open) {
            setEditCompany(null);
            setEditError(null);
          }
        }}
      >
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
          <DialogPrimitive.Content
            className={cn(
              "data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-lg border bg-background p-6 shadow-lg",
            )}
          >
            <DialogPrimitive.Title className="text-lg font-semibold">{tMc("editCompanyTitle")}</DialogPrimitive.Title>
            <DialogPrimitive.Description className="text-sm text-muted-foreground mt-1">
              {tMc("editCompanyDescription", { name: editCompany?.name?.trim() || tMc("editCompanyFallbackName") })}
            </DialogPrimitive.Description>
            <div className="mt-4 flex flex-col gap-3">
              <div className="space-y-2">
                <Label htmlFor="edit-max-users">{tMc("maxUsersLabel")}</Label>
                <Input
                  id="edit-max-users"
                  type="number"
                  min={1}
                  value={editMaxUsers}
                  onChange={(e) => setEditMaxUsers(e.target.value)}
                  placeholder={tMc("placeholderUnchanged")}
                  disabled={!!savingCompanyId}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-max-admins">{tMc("maxAdminsLabel")}</Label>
                <Input
                  id="edit-max-admins"
                  type="number"
                  min={1}
                  value={editMaxAdmins}
                  onChange={(e) => setEditMaxAdmins(e.target.value)}
                  placeholder={tMc("placeholderUnchanged")}
                  disabled={!!savingCompanyId}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-subscription">{tMc("editSubscriptionTitle")}</Label>
                <select
                  id="edit-subscription"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={editSubscriptionPlanId}
                  onChange={(e) => setEditSubscriptionPlanId(e.target.value)}
                  disabled={!!savingCompanyId}
                >
                  {editSubscriptionPlanId &&
                    !subscriptionPlans.some((p) => p.id === editSubscriptionPlanId) && (
                      <option value={editSubscriptionPlanId}>{planDisplayName(editSubscriptionPlanId)}</option>
                    )}
                  {subscriptionPlans.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} ({p.kind})
                      {p.isActive ? "" : tMc("subscriptionInactiveSuffix")}
                    </option>
                  ))}
                </select>
              </div>
              {editCompany && (editCompany.adminCount ?? 0) === 0 && (
                <div className="space-y-3 rounded-md border p-3">
                  <p className="text-sm font-medium">{tMc("inviteEmailsEditLabel")}</p>
                  <p className="text-xs text-muted-foreground">{tMc("inviteEmailsEditHelp")}</p>
                  <div className="flex flex-col gap-2">
                    {editInviteEmails.map((em, i) => (
                      <Input
                        key={i}
                        type="email"
                        value={em}
                        onChange={(e) => setEditInviteEmailAt(i, e.target.value)}
                        placeholder={tMc("inviteEmailPlaceholder", { n: i + 1 })}
                        disabled={!!savingCompanyId}
                        autoComplete="off"
                      />
                    ))}
                  </div>
                  <label className="flex cursor-pointer items-start gap-2 text-sm">
                    <input
                      type="checkbox"
                      className="mt-1 h-4 w-4 rounded border"
                      checked={editResendInvite}
                      onChange={(e) => setEditResendInvite(e.target.checked)}
                      disabled={!!savingCompanyId}
                    />
                    <span>{tMc("inviteResendAfterSave")}</span>
                  </label>
                </div>
              )}
              {editError && (
                <p className="text-sm text-destructive" role="alert">
                  {editError}
                </p>
              )}
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <DialogPrimitive.Close asChild>
                <Button type="button" variant="outline" disabled={!!savingCompanyId}>
                  {tMc("cancelButton")}
                </Button>
              </DialogPrimitive.Close>
              <Button
                type="button"
                onClick={() => void handleSaveCompany()}
                disabled={!!savingCompanyId}
              >
                {savingCompanyId ? tMc("savingButton") : tMc("saveButton")}
              </Button>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>

      <DialogPrimitive.Root
        open={!!reductionFlow}
        onOpenChange={(open) => {
          if (!open) setReductionFlow(null);
        }}
      >
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
          <DialogPrimitive.Content
            className={cn(
              "data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed left-[50%] top-[50%] z-[60] max-h-[85vh] w-full max-w-lg translate-x-[-50%] translate-y-[-50%] overflow-y-auto rounded-lg border bg-background p-6 shadow-lg",
            )}
          >
            <DialogPrimitive.Title className="text-lg font-semibold">{tMc("reductionTitle")}</DialogPrimitive.Title>
            <DialogPrimitive.Description className="text-sm text-muted-foreground mt-1">
              {tMc("reductionDescription")}
            </DialogPrimitive.Description>
            {reductionFlow && (
              <div className="mt-3 text-xs text-muted-foreground space-y-1">
                <p>
                  Current: {reductionFlow.details.activeUserCount} active users
                  {reductionFlow.details.proposedMaxUserAccounts != null
                    ? ` → cap ${reductionFlow.details.proposedMaxUserAccounts}`
                    : ""}
                  {reductionFlow.details.minimumUsersToDeactivate > 0
                    ? ` — deactivate at least ${reductionFlow.details.minimumUsersToDeactivate}.`
                    : ""}
                </p>
                <p>
                  Current: {reductionFlow.details.adminCount} admins
                  {reductionFlow.details.proposedMaxAdminAccounts != null
                    ? ` → cap ${reductionFlow.details.proposedMaxAdminAccounts}`
                    : ""}
                  {reductionFlow.details.minimumAdminsToDemote > 0
                    ? ` — demote at least ${reductionFlow.details.minimumAdminsToDemote}.`
                    : ""}
                </p>
              </div>
            )}
            {reductionUsersLoading ? (
              <p className="mt-4 text-sm text-muted-foreground">Loading users…</p>
            ) : (
              <div className="mt-4 flex flex-col gap-4">
                {reductionFlow && reductionFlow.details.minimumAdminsToDemote > 0 && (
                  <div className="space-y-2">
                    <p className="text-sm font-medium">Demote from Admin to User</p>
                    <ul className="max-h-40 space-y-2 overflow-y-auto rounded border p-2">
                      {reductionUsers.filter((u) => u.isActive && isCompanyAdmin(u)).length === 0 ? (
                        <li className="text-sm text-muted-foreground">No active administrators listed.</li>
                      ) : (
                        reductionUsers
                          .filter((u) => u.isActive && isCompanyAdmin(u))
                          .map((u) => (
                            <li key={u.userId} className="flex items-center gap-2 text-sm">
                              <input
                                type="checkbox"
                                id={`demote-${u.userId}`}
                                checked={selectedDemote.has(u.userId)}
                                onChange={() => toggleDemote(u.userId)}
                                className="h-4 w-4 rounded border"
                              />
                              <label htmlFor={`demote-${u.userId}`} className="cursor-pointer">
                                {u.email || u.displayName || u.userId}
                              </label>
                            </li>
                          ))
                      )}
                    </ul>
                  </div>
                )}
                {reductionFlow && reductionFlow.details.minimumUsersToDeactivate > 0 && (
                  <div className="space-y-2">
                    <p className="text-sm font-medium">Deactivate accounts</p>
                    <ul className="max-h-40 space-y-2 overflow-y-auto rounded border p-2">
                      {reductionUsers.filter((u) => u.isActive).length === 0 ? (
                        <li className="text-sm text-muted-foreground">No active users.</li>
                      ) : (
                        reductionUsers
                          .filter((u) => u.isActive)
                          .map((u) => (
                            <li key={`de-${u.userId}`} className="flex items-center gap-2 text-sm">
                              <input
                                type="checkbox"
                                id={`deact-${u.userId}`}
                                checked={selectedDeactivate.has(u.userId)}
                                onChange={() => toggleDeactivate(u.userId)}
                                className="h-4 w-4 rounded border"
                              />
                              <label htmlFor={`deact-${u.userId}`} className="cursor-pointer">
                                {u.email || u.displayName || u.userId}
                                {isCompanyAdmin(u) ? " (admin)" : ""}
                              </label>
                            </li>
                          ))
                      )}
                    </ul>
                  </div>
                )}
              </div>
            )}
            {reductionError && (
              <p className="mt-3 text-sm text-destructive" role="alert">
                {reductionError}
              </p>
            )}
            <div className="mt-6 flex justify-end gap-2">
              <DialogPrimitive.Close asChild>
                <Button type="button" variant="outline" disabled={applyingReduction}>
                  {tMc("cancelButton")}
                </Button>
              </DialogPrimitive.Close>
              <Button type="button" onClick={() => void handleApplyReduction()} disabled={applyingReduction || reductionUsersLoading}>
                {applyingReduction ? tMc("reductionApplying") : tMc("reductionApply")}
              </Button>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </div>
  );
}
