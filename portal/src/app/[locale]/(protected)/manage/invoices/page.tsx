"use client";

import { useAuth } from "@/context/AuthContext";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import {
  adminDownloadBillingPeriodInvoicePdf,
  adminGetBillingBreakdownByDateRange,
  adminGetBillingPeriodDetail,
  adminGetCompanies,
  adminGetUsers,
  adminSendBillingPeriodInvoiceEmail,
  adminSetBookingInvoiceExcluded,
  type AdminCompany,
  type AdminUser,
  type BillingMonthBreakdown,
  type BillingPeriodDetail,
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

/** UTC calendar yyyy-MM-dd */
function utcYmd(d: Date): string {
  const y = d.getUTCFullYear();
  const m = String(d.getUTCMonth() + 1).padStart(2, "0");
  const day = String(d.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function defaultUtcDateRange(): { from: string; to: string } {
  const today = new Date();
  const start = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
  return { from: utcYmd(start), to: utcYmd(today) };
}

export default function ManageInvoicesPage() {
  const { token } = useAuth();
  const t = useTranslations("manageInvoices");
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [selectedCompanyId, setSelectedCompanyId] = useState("");
  const [{ from: dateFrom, to: dateTo }, setDateRange] = useState(defaultUtcDateRange);
  const [breakdown, setBreakdown] = useState<BillingMonthBreakdown | null>(null);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingBreakdown, setLoadingBreakdown] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lineDetail, setLineDetail] = useState<BillingPeriodDetail | null>(null);
  const [lineDetailLoading, setLineDetailLoading] = useState(false);
  const [invoiceAdmins, setInvoiceAdmins] = useState<AdminUser[]>([]);
  const [invoiceAdminsLoading, setInvoiceAdminsLoading] = useState(false);
  const [emailRecipientId, setEmailRecipientId] = useState("");
  const [pdfBusy, setPdfBusy] = useState(false);
  const [emailBusy, setEmailBusy] = useState(false);
  const [bookingBusyId, setBookingBusyId] = useState<string | null>(null);

  const selectedBusinessId = useMemo(() => {
    const c = companies.find((x) => x.id === selectedCompanyId);
    return c?.businessId?.trim() || null;
  }, [companies, selectedCompanyId]);

  const invoicePeriodId = breakdown?.billingPeriodId ?? null;
  const invoiceActionsAvailable = invoicePeriodId != null && invoicePeriodId.length > 0;

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
    setBreakdown(null);
    setDateRange(defaultUtcDateRange());
  }, [selectedCompanyId]);

  const loadBreakdown = useCallback(() => {
    if (!token || !selectedCompanyId) return;
    if (!dateFrom || !dateTo) {
      setError(t("datesRequired"));
      return;
    }
    if (dateFrom > dateTo) {
      setError(t("endBeforeStart"));
      return;
    }
    setLoadingBreakdown(true);
    setError(null);
    adminGetBillingBreakdownByDateRange(token, selectedCompanyId, dateFrom, dateTo)
      .then(setBreakdown)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load breakdown"))
      .finally(() => setLoadingBreakdown(false));
  }, [token, selectedCompanyId, dateFrom, dateTo, t]);

  useEffect(() => {
    if (!token || !breakdown || !selectedBusinessId) {
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
  }, [token, breakdown, selectedBusinessId]);

  const openLineDetail = () => {
    if (!token || !invoicePeriodId) return;
    setLineDetailLoading(true);
    setLineDetail(null);
    setError(null);
    adminGetBillingPeriodDetail(token, invoicePeriodId)
      .then(setLineDetail)
      .catch((e) => {
        setLineDetail(null);
        setError(e instanceof Error ? e.message : "Failed to load period lines");
      })
      .finally(() => setLineDetailLoading(false));
  };

  const onDownloadPdf = async () => {
    if (!token || !invoicePeriodId) return;
    setPdfBusy(true);
    setError(null);
    try {
      const blob = await adminDownloadBillingPeriodInvoicePdf(token, invoicePeriodId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `invoice-${dateFrom}-${dateTo}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      setError(e instanceof Error ? e.message : "PDF download failed");
    } finally {
      setPdfBusy(false);
    }
  };

  const onSendEmail = async () => {
    if (!token || !invoicePeriodId || !emailRecipientId) return;
    setEmailBusy(true);
    setError(null);
    try {
      await adminSendBillingPeriodInvoiceEmail(token, invoicePeriodId, emailRecipientId);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Send email failed");
    } finally {
      setEmailBusy(false);
    }
  };

  const onToggleBookingExcluded = async (bookingId: string, excluded: boolean) => {
    if (!token || !invoicePeriodId) return;
    setBookingBusyId(bookingId);
    setError(null);
    try {
      await adminSetBookingInvoiceExcluded(token, invoicePeriodId, bookingId, excluded);
      await loadBreakdown();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setBookingBusyId(null);
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
          <CardTitle>{t("breakdownTitle")}</CardTitle>
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

          {selectedCompanyId ? (
            <div className="flex flex-wrap items-end gap-4 max-w-2xl">
              <div className="space-y-2">
                <Label htmlFor="invoice-from">{t("periodStart")}</Label>
                <input
                  id="invoice-from"
                  type="date"
                  className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={dateFrom}
                  onChange={(e) => setDateRange((r) => ({ ...r, from: e.target.value }))}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="invoice-to">{t("periodEnd")}</Label>
                <input
                  id="invoice-to"
                  type="date"
                  className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={dateTo}
                  onChange={(e) => setDateRange((r) => ({ ...r, to: e.target.value }))}
                />
              </div>
              <Button type="button" onClick={loadBreakdown} disabled={loadingBreakdown}>
                {loadingBreakdown ? "…" : t("loadBreakdown")}
              </Button>
            </div>
          ) : null}

          <p className="text-xs text-muted-foreground max-w-2xl">{t("periodUtcHint")}</p>

          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}

          {loadingBreakdown && <p className="text-sm text-muted-foreground">{t("loadingBreakdown")}</p>}

          {breakdown && !loadingBreakdown ? (
            <div className="space-y-6">
              {breakdown.billableBookingCount === 0 ? (
                <p className="text-sm text-muted-foreground">{t("noBookingsInRange")}</p>
              ) : null}

              <div className="flex flex-wrap gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">{t("totalPayable")}: </span>
                  <span className="font-semibold">{formatMoney(breakdown.payableTotal, breakdown.currency)}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">{t("totalLedger")}: </span>
                  <span>{formatMoney(breakdown.ledgerTotal, breakdown.currency)}</span>
                </div>
              </div>

              {!invoiceActionsAvailable && breakdown.billableBookingCount > 0 ? (
                <p className="text-sm text-amber-700 dark:text-amber-500">{t("multiMonthInvoiceHint")}</p>
              ) : null}

              {breakdown.segments.length > 0 ? (
                <div>
                  <h3 className="text-sm font-medium mb-2">{t("segmentsTitle")}</h3>
                  <div className="overflow-x-auto rounded-md border">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b bg-muted/50">
                          <th className="p-2 text-left font-medium">{t("segmentLabel")}</th>
                          <th className="p-2 text-right font-medium">{t("segmentBookings")}</th>
                          <th className="p-2 text-right font-medium">{t("segmentUnit")}</th>
                          <th className="p-2 text-right font-medium">{t("segmentSubtotal")}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {breakdown.segments.map((s, i) => (
                          <tr key={`${s.label}-${i}`} className="border-b last:border-0">
                            <td className="p-2">{s.label}</td>
                            <td className="p-2 text-right">{s.bookingCount}</td>
                            <td className="p-2 text-right">
                              {s.unitRate != null ? formatMoney(s.unitRate, breakdown.currency) : "—"}
                            </td>
                            <td className="p-2 text-right">{formatMoney(s.subtotal, breakdown.currency)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ) : null}

              <div>
                <h3 className="text-sm font-medium mb-2">{t("bookingsTitle")}</h3>
                <div className="overflow-x-auto rounded-md border">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="p-2 text-left font-medium">{t("bookingRef")}</th>
                        <th className="p-2 text-left font-medium">{t("plan")}</th>
                        <th className="p-2 text-left font-medium">{t("bookingDescription")}</th>
                        <th className="p-2 text-right font-medium">{t("amount")}</th>
                        <th className="p-2 text-left font-medium">{t("excludeBooking")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {breakdown.bookings.map((b) => (
                        <tr key={b.bookingId} className="border-b last:border-0">
                          <td className="p-2 font-mono text-xs">
                            {b.shipmentNumber || b.referenceNumber || b.bookingId.slice(0, 8)}
                          </td>
                          <td className="p-2">{b.planLabel || "—"}</td>
                          <td className="p-2 text-muted-foreground">{b.description}</td>
                          <td className="p-2 text-right">{formatMoney(b.amount, breakdown.currency)}</td>
                          <td className="p-2">
                            <Switch
                              checked={b.excludedFromInvoice}
                              disabled={!invoiceActionsAvailable || bookingBusyId === b.bookingId}
                              onCheckedChange={(v) => onToggleBookingExcluded(b.bookingId, v)}
                              aria-label={t("excludeBooking")}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="flex flex-wrap items-end gap-3 border-t pt-4">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={pdfBusy || !invoiceActionsAvailable}
                  onClick={onDownloadPdf}
                >
                  {pdfBusy ? "…" : t("saveInvoice")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={!invoiceActionsAvailable}
                  onClick={openLineDetail}
                >
                  {t("viewLines")}
                </Button>
                <div className="flex flex-col gap-1 min-w-[200px]">
                  <Label htmlFor="invoice-email-recipient">{t("recipientAdmin")}</Label>
                  <select
                    id="invoice-email-recipient"
                    className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                    value={emailRecipientId}
                    onChange={(e) => setEmailRecipientId(e.target.value)}
                    disabled={
                      !invoiceActionsAvailable || !selectedBusinessId || invoiceAdminsLoading || invoiceAdmins.length === 0
                    }
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
                    !invoiceActionsAvailable ||
                    !emailRecipientId ||
                    !selectedBusinessId ||
                    invoiceAdmins.length === 0
                  }
                  onClick={onSendEmail}
                >
                  {emailBusy ? "…" : t("sendInvoice")}
                </Button>
              </div>
              {!invoiceActionsAvailable && (
                <p className="text-xs text-muted-foreground">{t("invoiceActionsSingleMonth")}</p>
              )}
              {!selectedBusinessId && (
                <p className="text-xs text-muted-foreground">{t("emailRequiresBusinessId")}</p>
              )}
            </div>
          ) : null}
        </CardContent>
      </Card>

      <DialogPrimitive.Root
        open={lineDetail != null || lineDetailLoading}
        onOpenChange={(open) => {
          if (!open) {
            setLineDetail(null);
            setLineDetailLoading(false);
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
            {lineDetailLoading && <p className="mt-4 text-sm text-muted-foreground">…</p>}
            {lineDetail && (
              <div className="mt-4 overflow-x-auto rounded-md border">
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
                    {lineDetail.lineItems.map((l) => (
                      <tr key={l.id} className="border-b last:border-0">
                        <td className="p-2">{l.lineType}</td>
                        <td className="p-2 font-mono text-xs">{l.bookingId ?? "—"}</td>
                        <td className="p-2 text-right">{formatMoney(l.amount, l.currency)}</td>
                        <td className="p-2">
                          {l.excludedFromInvoice ? t("excludedYes") : t("excludedNo")}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
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
