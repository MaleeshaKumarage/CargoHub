"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import {
  adminGetCompanies,
  adminSendReleaseNotes,
  adminSendTestEmail,
  type AdminCompany,
} from "@/lib/api";
import { releaseNotesEmailBodyHtml } from "@/lib/releaseNotesEmailHtml";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const ROLE_KEYS = ["SuperAdmin", "Admin", "User"] as const;

export default function ManageReleaseNotesPage() {
  const { token } = useAuth();
  const t = useTranslations("manageReleaseNotes");

  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [allCompanies, setAllCompanies] = useState(true);
  const [selectedCompanyIds, setSelectedCompanyIds] = useState<Set<string>>(new Set());
  const [allRoles, setAllRoles] = useState(true);
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set());

  const [sending, setSending] = useState(false);
  const [sendMessage, setSendMessage] = useState<string | null>(null);
  const [sendIsError, setSendIsError] = useState(false);

  const [testEmailTo, setTestEmailTo] = useState("");
  const [testEmailSending, setTestEmailSending] = useState(false);
  const [testEmailMessage, setTestEmailMessage] = useState<string | null>(null);
  const [testEmailIsError, setTestEmailIsError] = useState(false);

  useEffect(() => {
    if (!token) return;
    let c = false;
    setLoadingCompanies(true);
    setLoadError(null);
    adminGetCompanies(token)
      .then((list) => {
        if (!c) setCompanies(list);
      })
      .catch((e) => {
        if (!c) setLoadError(e instanceof Error ? e.message : "Failed to load companies");
      })
      .finally(() => {
        if (!c) setLoadingCompanies(false);
      });
    return () => {
      c = true;
    };
  }, [token]);

  const previewHtml = useMemo(() => releaseNotesEmailBodyHtml(body), [body]);

  function toggleCompany(id: string) {
    setSelectedCompanyIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function toggleRole(role: string) {
    setSelectedRoles((prev) => {
      const next = new Set(prev);
      if (next.has(role)) next.delete(role);
      else next.add(role);
      return next;
    });
  }

  async function handleSend(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    const sub = subject.trim();
    const msg = body;
    if (!sub) {
      setSendIsError(true);
      setSendMessage(t("validationSubject"));
      return;
    }
    if (!msg.trim()) {
      setSendIsError(true);
      setSendMessage(t("validationBody"));
      return;
    }
    if (!allCompanies && selectedCompanyIds.size === 0) {
      setSendIsError(true);
      setSendMessage(t("validationCompanies"));
      return;
    }
    if (!allRoles && selectedRoles.size === 0) {
      setSendIsError(true);
      setSendMessage(t("validationRoles"));
      return;
    }

    setSending(true);
    setSendMessage(null);
    setSendIsError(false);
    try {
      const r = await adminSendReleaseNotes(token, {
        subject: sub,
        body: msg,
        allCompanies,
        companyIds: allCompanies ? undefined : [...selectedCompanyIds],
        allRoles,
        roles: allRoles ? undefined : [...selectedRoles],
      });
      const failed = r.failures.length;
      if (failed > 0) {
        setSendIsError(true);
        const detail = r.failures.map((f) => `${f.email}: ${f.message}`).join("\n");
        setSendMessage(`${t("resultPartial", { sent: r.sentCount, total: r.recipientCount })}\n${detail}`);
      } else {
        setSendIsError(false);
        setSendMessage(t("resultOk", { sent: r.sentCount, total: r.recipientCount }));
      }
    } catch (err) {
      setSendIsError(true);
      setSendMessage(err instanceof Error ? err.message : "Failed");
    } finally {
      setSending(false);
    }
  }

  async function handleTestEmail(ev: React.FormEvent) {
    ev.preventDefault();
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
      setTestEmailMessage(r.message ?? "Sent.");
    } catch (e) {
      setTestEmailIsError(true);
      setTestEmailMessage(e instanceof Error ? e.message : "Failed");
    } finally {
      setTestEmailSending(false);
    }
  }

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground mt-1">{t("description")}</p>
      </div>

      {loadError && (
        <p className="text-sm text-destructive" role="alert">
          {loadError}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>{t("composeTitle")}</CardTitle>
          <CardDescription>{t("composeDescription")}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSend} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="rn-subject">{t("subject")}</Label>
              <Input
                id="rn-subject"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                disabled={sending}
                className="max-w-xl"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="rn-body">{t("body")}</Label>
              <textarea
                id="rn-body"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                disabled={sending}
                rows={12}
                className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring flex min-h-[200px] w-full max-w-3xl rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
            <div className="space-y-2">
              <p className="text-sm font-medium">{t("preview")}</p>
              <div
                className="bg-muted/40 max-w-3xl rounded-md border p-3 text-sm"
                dangerouslySetInnerHTML={{ __html: previewHtml }}
              />
            </div>

            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-3">
                <label className="flex cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={allCompanies}
                    onChange={(e) => setAllCompanies(e.target.checked)}
                    disabled={sending || loadingCompanies}
                    className="h-4 w-4 rounded border"
                  />
                  {t("companiesAll")}
                </label>
              </div>
              {!allCompanies && (
                <div className="max-h-48 space-y-2 overflow-y-auto rounded border p-3">
                  {companies.length === 0 ? (
                    <p className="text-muted-foreground text-sm">{t("noCompanies")}</p>
                  ) : (
                    companies.map((c) => (
                      <label key={c.id} className="flex cursor-pointer items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          checked={selectedCompanyIds.has(c.id)}
                          onChange={() => toggleCompany(c.id)}
                          disabled={sending}
                          className="h-4 w-4 rounded border"
                        />
                        <span>
                          {c.name ?? c.companyId}
                          {c.businessId ? ` (${c.businessId})` : ""}
                        </span>
                      </label>
                    ))
                  )}
                </div>
              )}
            </div>

            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-3">
                <label className="flex cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={allRoles}
                    onChange={(e) => setAllRoles(e.target.checked)}
                    disabled={sending}
                    className="h-4 w-4 rounded border"
                  />
                  {t("rolesAll")}
                </label>
              </div>
              {!allRoles && (
                <div className="flex flex-wrap gap-4 rounded border p-3">
                  {ROLE_KEYS.map((role) => (
                    <label key={role} className="flex cursor-pointer items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={selectedRoles.has(role)}
                        onChange={() => toggleRole(role)}
                        disabled={sending}
                        className="h-4 w-4 rounded border"
                      />
                      {role === "SuperAdmin"
                        ? t("roleSuperAdmin")
                        : role === "Admin"
                          ? t("roleAdmin")
                          : t("roleUser")}
                    </label>
                  ))}
                </div>
              )}
            </div>

            <Button type="submit" disabled={sending || loadingCompanies}>
              {sending ? t("sending") : t("send")}
            </Button>
            {sendMessage && (
              <p
                className={`text-sm whitespace-pre-wrap ${sendIsError ? "text-destructive" : "text-muted-foreground"}`}
                role={sendIsError ? "alert" : "status"}
              >
                {sendMessage}
              </p>
            )}
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t("smtpTitle")}</CardTitle>
          <CardDescription>{t("smtpDescription")}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleTestEmail} className="flex flex-wrap items-end gap-3">
            <div className="space-y-2">
              <Label htmlFor="rn-test-email">{t("smtpRecipient")}</Label>
              <Input
                id="rn-test-email"
                type="email"
                value={testEmailTo}
                onChange={(e) => setTestEmailTo(e.target.value)}
                placeholder="you@example.com"
                className="w-[280px]"
                disabled={testEmailSending}
              />
            </div>
            <Button type="submit" variant="secondary" disabled={testEmailSending}>
              {testEmailSending ? t("smtpSending") : t("smtpSend")}
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
