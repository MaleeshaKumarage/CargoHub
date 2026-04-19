"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { Dialog as DialogPrimitive } from "radix-ui";
import { cn } from "@/lib/utils";
import {
  adminGetCompanies,
  adminCreateFreelanceRider,
  adminListFreelanceRiders,
  adminPatchFreelanceRider,
  adminSendFreelanceRiderInvite,
  type AdminCompany,
  type AdminFreelanceRider,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";

function parsePostalCodesInput(raw: string): string[] {
  return raw
    .split(/[\s,;]+/g)
    .map((x) => x.trim())
    .filter(Boolean);
}

export default function ManageFreelanceRidersPage() {
  const { token } = useAuth();
  const t = useTranslations("manageFreelanceRiders");
  const [list, setList] = useState<AdminFreelanceRider[]>([]);
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [newBusinessId, setNewBusinessId] = useState("");
  const [newDisplayName, setNewDisplayName] = useState("");
  const [newPhone, setNewPhone] = useState("");
  const [newEmail, setNewEmail] = useState("");
  const [newCompanyId, setNewCompanyId] = useState("");
  const [newPostals, setNewPostals] = useState("");
  const [newSendInvite, setNewSendInvite] = useState(true);

  const [edit, setEdit] = useState<AdminFreelanceRider | null>(null);
  const [editDisplayName, setEditDisplayName] = useState("");
  const [editPhone, setEditPhone] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editStatus, setEditStatus] = useState("Active");
  const [editCompanyId, setEditCompanyId] = useState("");
  const [editPostals, setEditPostals] = useState("");
  const [saving, setSaving] = useState(false);
  const [inviteId, setInviteId] = useState<string | null>(null);

  const companyOptions = useMemo(
    () => companies.map((c) => ({ id: c.id, label: c.name || c.businessId || c.companyId })),
    [companies],
  );

  function reload() {
    if (!token) return;
    setError(null);
    setLoading(true);
    Promise.all([adminListFreelanceRiders(token), adminGetCompanies(token)])
      .then(([riders, comps]) => {
        setList(riders);
        setCompanies(comps);
      })
      .catch(() => setError(t("loadError")))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    reload();
  }, [token]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setCreating(true);
    setError(null);
    try {
      await adminCreateFreelanceRider(token, {
        businessId: newBusinessId.trim(),
        displayName: newDisplayName.trim() || undefined,
        phone: newPhone.trim() || undefined,
        email: newEmail.trim(),
        companyId: newCompanyId || undefined,
        postalCodes: parsePostalCodesInput(newPostals),
        sendInvite: newSendInvite,
      });
      setNewBusinessId("");
      setNewDisplayName("");
      setNewPhone("");
      setNewEmail("");
      setNewCompanyId("");
      setNewPostals("");
      reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("loadError"));
    } finally {
      setCreating(false);
    }
  }

  function openEdit(r: AdminFreelanceRider) {
    setEdit(r);
    setEditDisplayName(r.displayName ?? "");
    setEditPhone(r.phone ?? "");
    setEditEmail(r.email ?? "");
    setEditStatus(r.status || "Active");
    setEditCompanyId(r.companyId ?? "");
    setEditPostals((r.postalCodes ?? []).join("\n"));
  }

  async function saveEdit() {
    if (!token || !edit) return;
    setSaving(true);
    setError(null);
    try {
      const patchBody: Parameters<typeof adminPatchFreelanceRider>[2] = {
        displayName: editDisplayName,
        phone: editPhone,
        email: editEmail,
        status: editStatus,
        postalCodes: parsePostalCodesInput(editPostals),
      };
      if (editCompanyId.trim()) {
        patchBody.companyId = editCompanyId.trim();
      } else if (edit.companyId) {
        patchBody.clearCompany = true;
      }
      await adminPatchFreelanceRider(token, edit.id, patchBody);
      setEdit(null);
      reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("loadError"));
    } finally {
      setSaving(false);
    }
  }

  async function sendInvite(id: string) {
    if (!token) return;
    setInviteId(id);
    setError(null);
    try {
      await adminSendFreelanceRiderInvite(token, id);
      reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("loadError"));
    } finally {
      setInviteId(null);
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground mt-1">{t("description")}</p>
      </div>
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>{t("createTitle")}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleCreate} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="space-y-2 sm:col-span-1">
              <Label htmlFor="nBid">{t("businessId")}</Label>
              <Input
                id="nBid"
                value={newBusinessId}
                onChange={(e) => setNewBusinessId(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="nDn">{t("displayName")}</Label>
              <Input id="nDn" value={newDisplayName} onChange={(e) => setNewDisplayName(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="nPh">{t("phone")}</Label>
              <Input id="nPh" value={newPhone} onChange={(e) => setNewPhone(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="nEm">{t("email")}</Label>
              <Input id="nEm" type="email" value={newEmail} onChange={(e) => setNewEmail(e.target.value)} required />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="nCo">{t("companyOptional")}</Label>
              <select
                id="nCo"
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                value={newCompanyId}
                onChange={(e) => setNewCompanyId(e.target.value)}
              >
                <option value="">{t("companyNone")}</option>
                {companyOptions.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2 sm:col-span-2 lg:col-span-3">
              <Label htmlFor="nPc">{t("postalCodes")}</Label>
              <textarea
                id="nPc"
                rows={3}
                value={newPostals}
                onChange={(e) => setNewPostals(e.target.value)}
                className={cn(
                  "flex min-h-[72px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
                )}
              />
              <p className="text-xs text-muted-foreground">{t("postalCodesHint")}</p>
            </div>
            <div className="flex items-center gap-2 sm:col-span-2">
              <Switch id="nInv" checked={newSendInvite} onCheckedChange={setNewSendInvite} />
              <Label htmlFor="nInv">{t("sendInvite")}</Label>
            </div>
            <div className="sm:col-span-2 lg:col-span-3">
              <Button type="submit" disabled={creating}>
                {creating ? t("creating") : t("create")}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t("title")}</CardTitle>
          <CardDescription>{list.length === 0 ? t("listEmpty") : `${list.length}`}</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <p className="text-muted-foreground">Loading…</p>
          ) : list.length === 0 ? (
            <p className="text-muted-foreground">{t("listEmpty")}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left">
                    <th className="py-2 pr-4">{t("businessId")}</th>
                    <th className="py-2 pr-4">{t("email")}</th>
                    <th className="py-2 pr-4">{t("status")}</th>
                    <th className="py-2 pr-4">{t("companyOptional")}</th>
                    <th className="py-2 pr-4">{t("postalCodes")}</th>
                    <th className="py-2" />
                  </tr>
                </thead>
                <tbody>
                  {list.map((r) => (
                    <tr key={r.id} className="border-b border-border/60">
                      <td className="py-2 pr-4 align-top">{r.businessId}</td>
                      <td className="py-2 pr-4 align-top">{r.email}</td>
                      <td className="py-2 pr-4 align-top">{r.status}</td>
                      <td className="py-2 pr-4 align-top">{r.companyLabel ?? "—"}</td>
                      <td className="py-2 pr-4 align-top max-w-[12rem] truncate" title={(r.postalCodes ?? []).join(", ")}>
                        {(r.postalCodes ?? []).join(", ")}
                      </td>
                      <td className="py-2 align-top whitespace-nowrap space-x-2">
                        <Button type="button" variant="outline" size="sm" onClick={() => openEdit(r)}>
                          {t("edit")}
                        </Button>
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          disabled={inviteId === r.id}
                          onClick={() => sendInvite(r.id)}
                        >
                          {inviteId === r.id ? t("inviting") : t("invite")}
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <DialogPrimitive.Root open={!!edit} onOpenChange={(o) => !o && setEdit(null)}>
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
          <DialogPrimitive.Content
            className={cn(
              "data-[state=open]:animate-in data-[state=closed]:animate-out fixed left-[50%] top-[50%] z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto translate-x-[-50%] translate-y-[-50%] rounded-lg border bg-background p-6 shadow-lg",
            )}
          >
            <DialogPrimitive.Title className="text-lg font-semibold">{t("edit")}</DialogPrimitive.Title>
            <div className="mt-4 grid gap-3">
              <div className="space-y-2">
                <Label htmlFor="eDn">{t("displayName")}</Label>
                <Input id="eDn" value={editDisplayName} onChange={(e) => setEditDisplayName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="ePh">{t("phone")}</Label>
                <Input id="ePh" value={editPhone} onChange={(e) => setEditPhone(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="eEm">{t("email")}</Label>
                <Input id="eEm" type="email" value={editEmail} onChange={(e) => setEditEmail(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="eSt">{t("status")}</Label>
                <select
                  id="eSt"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                  value={editStatus}
                  onChange={(e) => setEditStatus(e.target.value)}
                >
                  {["PendingInvite", "Active", "Inactive"].map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="eCo">{t("companyOptional")}</Label>
                <select
                  id="eCo"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                  value={editCompanyId}
                  onChange={(e) => setEditCompanyId(e.target.value)}
                >
                  <option value="">{t("companyNone")}</option>
                  {companyOptions.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="ePc">{t("postalCodes")}</Label>
                <textarea
                  id="ePc"
                  rows={4}
                  value={editPostals}
                  onChange={(e) => setEditPostals(e.target.value)}
                  className={cn(
                    "flex min-h-[96px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm",
                    "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
                  )}
                />
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <DialogPrimitive.Close asChild>
                <Button type="button" variant="outline">
                  {t("cancel")}
                </Button>
              </DialogPrimitive.Close>
              <Button type="button" onClick={saveEdit} disabled={saving}>
                {saving ? t("saving") : t("save")}
              </Button>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </div>
  );
}
