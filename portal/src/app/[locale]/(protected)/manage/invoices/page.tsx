"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import {
  adminDownloadBillingPeriodInvoicePdf,
  adminGetBillingPeriodDetail,
  adminGetCompanies,
  adminGetCompanyBillingPeriods,
  adminGetUsers,
  adminPatchBillingLineItem,
  adminSendBillingPeriodInvoiceEmail,
  type AdminCompany,
  type AdminUser,
  type BillingPeriodDetail,
  type CompanyBillingPeriodSummary,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Dialog as DialogPrimitive } from "radix-ui";
import { cn } from "@/lib/utils";

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency: currency || "EUR" }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

export default function ManageInvoicesPage() {
  const { token } = useAuth();
  const t = useTranslations("manageInvoices");
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [selectedCompanyId, setSelectedCompanyId] = useState("");
  const [periods, setPeriods] = useState<CompanyBillingPeriodSummary[]>([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingPeriods, setLoadingPeriods] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<BillingPeriodDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [openPeriodId, setOpenPeriodId] = useState<string | null>(null);
  const [invoiceAdmins, setInvoiceAdmins] = useState<AdminUser[]>([]);
  const [invoiceAdminsLoading, setInvoiceAdminsLoading] = useState(false);
  const [emailRecipientId, setEmailRecipientId] = useState("");
  const [pdfBusy, setPdfBusy] = useState(false);
  const [emailBusy, setEmailBusy] = useState(false);
  const [lineBusyId, setLineBusyId] = useState<string | null>(null);

  const selectedBusinessId = useMemo(() => {
    const c = companies.find((x) => x.id === selectedCompanyId);
    return c?.businessId?.trim() || null;
  }, [companies, selectedCompanyId]);

  useEffect(() => {
    if (!token) return;
    let c = false;
    setLoadingCompanies(true);
    adminGetCompanies(token)
      .then((list) => {
        if (!c) setCompanies(list);
      })
      .catch((e) => {
        if (!c) setError(e instanceof Error ? e.message : "Failed to load companies");
      })
      .finally(() => {
        if (!c) setLoadingCompanies(false);
      });
    return () => {
      c = true;
    };
  }, [token]);

  useEffect(() => {
    if (!token || !selectedCompanyId) {
      setPeriods([]);
      return;
    }
    let c = false;
    setLoadingPeriods(true);
    setError(null);
    adminGetCompanyBillingPeriods(token, selectedCompanyId)
      .then((list) => {
        if (!c) setPeriods(list);
      })
      .catch((e) => {
        if (!c) setError(e instanceof Error ? e.message : "Failed to load periods");
      })
      .finally(() => {
        if (!c) setLoadingPeriods(false);
      });
    return () => {
      c = true;
    };
  }, [token, selectedCompanyId]);

  const openDetail = (periodId: string) => {
    if (!token) return;
    setOpenPeriodId(periodId);
    setDetailLoading(true);
    setDetail(null);
    setEmailRecipientId("");
    setInvoiceAdmins([]);
    adminGetBillingPeriodDetail(token, periodId)
      .then((d) => setDetail(d))
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load period"))
      .finally(() => setDetailLoading(false));
  };

  useEffect(() => {
    if (!token || !detail || !selectedBusinessId) {
      setInvoiceAdmins([]);
      return;
    }
    let c = false;
    setInvoiceAdminsLoading(true);
    adminGetUsers(token, selectedBusinessId)
      .then((users) => {
        if (!c) {
          const admins = users.filter(
            (u) => Array.isArray(u.roles) && u.roles.includes("Admin") && u.isActive
          );
          setInvoiceAdmins(admins);
        }
      })
      .catch(() => {
        if (!c) setInvoiceAdmins([]);
      })
      .finally(() => {
        if (!c) setInvoiceAdminsLoading(false);
      });
    return () => {
      c = true;
    };
  }, [token, detail?.id, selectedBusinessId]);

  const refreshDetail = () => {
    if (!token || !openPeriodId) return;
    adminGetBillingPeriodDetail(token, openPeriodId).then(setDetail).catch(() => {});
  };

  const onDownloadPdf = async () => {
    if (!token || !openPeriodId) return;
    setPdfBusy(true);
    setError(null);
    try {
      const blob = await adminDownloadBillingPeriodInvoicePdf(token, openPeriodId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `invoice-${detail?.yearUtc ?? ""}-${String(detail?.monthUtc ?? "").padStart(2, "0")}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e instanceof Error ? e.message : "PDF download failed");
    } finally {
      setPdfBusy(false);
    }
  };

  const onSendEmail = async () => {
    if (!token || !openPeriodId || !emailRecipientId) return;
    setEmailBusy(true);
    setError(null);
    try {
      await adminSendBillingPeriodInvoiceEmail(token, openPeriodId, emailRecipientId);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Send email failed");
    } finally {
      setEmailBusy(false);
    }
  };

  const onToggleExcluded = async (lineId: string, excluded: boolean) => {
    if (!token) return;
    setLineBusyId(lineId);
    setError(null);
    try {
      await adminPatchBillingLineItem(token, lineId, excluded);
      refreshDetail();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Update line failed");
    } finally {
      setLineBusyId(null);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground mt-1">{t("description")}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t("periodsTitle")}</CardTitle>
          <CardDescription>{t("selectCompany")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2 max-w-md">
            <Label htmlFor="invoice-company">{t("selectCompany")}</Label>
            <select
              id="invoice-company"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              disabled={loadingCompanies}
            >
              <option value="">{t("selectCompanyPlaceholder")}</option>
              {companies.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name ?? c.businessId ?? c.companyId}
                </option>
              ))}
            </select>
          </div>

          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}

          {!selectedCompanyId ? (
            <p className="text-sm text-muted-foreground">{t("selectCompanyPlaceholder")}</p>
          ) : loadingPeriods ? (
            <p className="text-sm text-muted-foreground">Loading…</p>
          ) : periods.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("noPeriods")}</p>
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left font-medium">{t("month")}</th>
                    <th className="p-3 text-left font-medium">{t("status")}</th>
                    <th className="p-3 text-right font-medium">{t("lines")}</th>
                    <th className="p-3 text-right font-medium">{t("payable")}</th>
                    <th className="p-3 text-left font-medium" />
                  </tr>
                </thead>
                <tbody>
                  {periods.map((p) => (
                    <tr key={p.id} className="border-b last:border-0">
                      <td className="p-3 font-mono">
                        {p.yearUtc}-{String(p.monthUtc).padStart(2, "0")}
                      </td>
                      <td className="p-3">{p.status}</td>
                      <td className="p-3 text-right">{p.lineItemCount}</td>
                      <td className="p-3 text-right">{formatMoney(p.payableTotal, p.currency)}</td>
                      <td className="p-3">
                        <Button type="button" variant="secondary" size="sm" onClick={() => openDetail(p.id)}>
                          {t("viewLines")}
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

      <DialogPrimitive.Root
        open={detail != null || detailLoading}
        onOpenChange={(open) => {
          if (!open) {
            setDetail(null);
            setDetailLoading(false);
          }
        }}
      >
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
          <DialogPrimitive.Content
            className={cn(
              "data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed left-[50%] top-[50%] z-50 max-h-[85vh] w-full max-w-3xl translate-x-[-50%] translate-y-[-50%] overflow-y-auto rounded-lg border bg-background p-6 shadow-lg",
            )}
          >
            <DialogPrimitive.Title className="text-lg font-semibold">{t("detailTitle")}</DialogPrimitive.Title>
            {detailLoading && <p className="mt-4 text-sm text-muted-foreground">…</p>}
            {detail && (
              <div className="mt-4 space-y-3">
                <p className="text-sm text-muted-foreground">
                  {detail.yearUtc}-{String(detail.monthUtc).padStart(2, "0")} · {detail.status} ·{" "}
                  {formatMoney(detail.payableTotal, detail.currency)} {t("payable").toLowerCase()}
                </p>
                <div className="flex flex-wrap items-end gap-3">
                  <Button type="button" variant="secondary" size="sm" disabled={pdfBusy} onClick={onDownloadPdf}>
                    {pdfBusy ? "…" : t("downloadPdf")}
                  </Button>
                  <div className="flex flex-col gap-1 min-w-[200px]">
                    <Label htmlFor="invoice-email-recipient">{t("recipientAdmin")}</Label>
                    <select
                      id="invoice-email-recipient"
                      className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                      value={emailRecipientId}
                      onChange={(e) => setEmailRecipientId(e.target.value)}
                      disabled={!selectedBusinessId || invoiceAdminsLoading || invoiceAdmins.length === 0}
                    >
                      <option value="">{t("recipientPlaceholder")}</option>
                      {invoiceAdmins.map((u) => (
                        <option key={u.userId} value={u.userId}>
                          {u.email || u.userId}
                        </option>
                      ))}
                    </select>
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    disabled={
                      emailBusy ||
                      !emailRecipientId ||
                      !selectedBusinessId ||
                      invoiceAdmins.length === 0
                    }
                    onClick={onSendEmail}
                  >
                    {emailBusy ? "…" : t("sendInvoice")}
                  </Button>
                </div>
                {!selectedBusinessId && (
                  <p className="text-xs text-muted-foreground">{t("emailRequiresBusinessId")}</p>
                )}
                <div className="overflow-x-auto rounded-md border">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="p-2 text-left font-medium">{t("lineType")}</th>
                        <th className="p-2 text-left font-medium">{t("booking")}</th>
                        <th className="p-2 text-right font-medium">{t("amount")}</th>
                        <th className="p-2 text-left font-medium">{t("excluded")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.lineItems.map((l) => (
                        <tr key={l.id} className="border-b last:border-0">
                          <td className="p-2">{l.lineType}</td>
                          <td className="p-2 font-mono text-xs">{l.bookingId ?? "—"}</td>
                          <td className="p-2 text-right">{formatMoney(l.amount, l.currency)}</td>
                          <td className="p-2">
                            <Switch
                              checked={l.excludedFromInvoice}
                              disabled={lineBusyId === l.id}
                              onCheckedChange={(v) => onToggleExcluded(l.id, v)}
                              aria-label={t("excluded")}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
            <div className="mt-6 flex justify-end">
              <DialogPrimitive.Close asChild>
                <Button type="button" variant="outline">
                  {t("close")}
                </Button>
              </DialogPrimitive.Close>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </div>
  );
}
