"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import {
  bookingList,
  bookingsExportDownload,
  bookingsImportAnalyze,
  bookingsImportApplyMapping,
  bookingsImportConfirm,
  bookingsWaybillsBulkDownload,
  draftList,
  type BookingImportPreview,
  type BookingImportResult,
  type BookingListItem,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BookingsDataTable } from "@/components/BookingsDataTable";
import { Dialog as DialogPrimitive } from "radix-ui";
import { cn } from "@/lib/utils";

type Tab = "completed" | "drafts";

type ImportDialogStep = "mapping" | "preview" | "summary";

export default function BookingsPage() {
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("bookings");
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [tab, setTab] = useState<Tab>("completed");
  const [list, setList] = useState<BookingListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exportLoading, setExportLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);
  const [waybillPdfLoading, setWaybillPdfLoading] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [importStep, setImportStep] = useState<ImportDialogStep>("preview");
  const [mappingSessionId, setMappingSessionId] = useState<string | null>(null);
  const [mappingFileHeaders, setMappingFileHeaders] = useState<string[]>([]);
  const [mappingBookingFields, setMappingBookingFields] = useState<string[]>([]);
  const [columnSelections, setColumnSelections] = useState<Record<string, string>>({});
  const [preview, setPreview] = useState<BookingImportPreview | null>(null);
  const [confirmCompleted, setConfirmCompleted] = useState(false);
  const [confirmDrafts, setConfirmDrafts] = useState(false);
  const [importResult, setImportResult] = useState<BookingImportResult | null>(null);
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;
    setLoading(true);
    setError(null);
    const load = tab === "drafts" ? draftList(token) : bookingList(token);
    load
      .then(setList)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, tab, isAuthenticated, isLoading]);

  if (!isAuthenticated || isLoading) return null;

  const isDrafts = tab === "drafts";
  const showImportExport = !isSuperAdmin && !isDrafts;

  const handleExport = async (format: "csv" | "xlsx") => {
    if (!token) return;
    setExportLoading(true);
    setError(null);
    try {
      await bookingsExportDownload(token, format);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Export failed");
    } finally {
      setExportLoading(false);
    }
  };

  const handleImportPick = () => fileInputRef.current?.click();

  const resetImportDialog = () => {
    setPreview(null);
    setImportResult(null);
    setImportStep("preview");
    setMappingSessionId(null);
    setMappingFileHeaders([]);
    setMappingBookingFields([]);
    setColumnSelections({});
    setConfirmCompleted(false);
    setConfirmDrafts(false);
  };

  const handleImportDialogOpenChange = (open: boolean) => {
    setImportDialogOpen(open);
    if (!open) resetImportDialog();
  };

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !token) return;
    setImportLoading(true);
    setError(null);
    try {
      const a = await bookingsImportAnalyze(token, file);
      setImportResult(null);
      if (!a.needsMapping) {
        if (a.totalDataRows === 0) {
          setError(t("importNoDataRows"));
          return;
        }
        setPreview({
          sessionId: a.sessionId,
          completedCount: a.completedCount,
          draftCount: a.draftCount,
          skippedEmptyRows: a.skippedEmptyRows,
          totalDataRows: a.totalDataRows,
        });
        setImportStep("preview");
      } else {
        setMappingSessionId(a.sessionId);
        setMappingFileHeaders(a.fileHeaders);
        setMappingBookingFields(a.bookingFields);
        const initial: Record<string, string> = {};
        for (const f of a.bookingFields) {
          initial[f] = a.fileHeaders.includes(f) ? f : "";
        }
        setColumnSelections(initial);
        setImportStep("mapping");
      }
      setConfirmCompleted(false);
      setConfirmDrafts(false);
      setImportDialogOpen(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setImportLoading(false);
    }
  };

  const canSubmitImport =
    !!preview &&
    ((confirmCompleted && preview.completedCount > 0) || (confirmDrafts && preview.draftCount > 0));

  const handleApplyColumnMapping = async () => {
    if (!token || !mappingSessionId) return;
    setImportLoading(true);
    setError(null);
    try {
      const columnMap: Record<string, string | null> = {};
      for (const f of mappingBookingFields) {
        const v = columnSelections[f]?.trim();
        columnMap[f] = v ? v : null;
      }
      const p = await bookingsImportApplyMapping(token, {
        sessionId: mappingSessionId,
        columnMap,
      });
      if (p.totalDataRows === 0) {
        setError(t("importNoDataRows"));
        return;
      }
      setPreview(p);
      setMappingSessionId(null);
      setMappingFileHeaders([]);
      setMappingBookingFields([]);
      setColumnSelections({});
      setConfirmCompleted(false);
      setConfirmDrafts(false);
      setImportStep("preview");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setImportLoading(false);
    }
  };

  const handleConfirmImport = async () => {
    if (!token || !preview || !canSubmitImport) return;
    setImportLoading(true);
    setError(null);
    try {
      const result = await bookingsImportConfirm(token, {
        sessionId: preview.sessionId,
        importCompleted: confirmCompleted,
        importDrafts: confirmDrafts,
      });
      setImportResult(result);
      setImportStep("summary");
      setLoading(true);
      bookingList(token)
        .then(setList)
        .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
        .finally(() => setLoading(false));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Import failed");
    } finally {
      setImportLoading(false);
    }
  };

  const handleBulkWaybills = async () => {
    if (!token || !importResult?.createdBookingIds.length) return;
    setWaybillPdfLoading(true);
    setError(null);
    try {
      await bookingsWaybillsBulkDownload(token, importResult.createdBookingIds);
    } catch (e) {
      setError(e instanceof Error ? e.message : "PDF failed");
    } finally {
      setWaybillPdfLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <input
        ref={fileInputRef}
        type="file"
        accept=".csv,.xlsx,.xls,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        className="hidden"
        aria-hidden
        onChange={handleImportFile}
      />

      <DialogPrimitive.Root open={importDialogOpen} onOpenChange={handleImportDialogOpenChange}>
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay
            className={cn(
              "fixed inset-0 z-50 bg-black/50 data-[state=open]:animate-in data-[state=closed]:animate-out",
            )}
          />
          <DialogPrimitive.Content
            className={cn(
              "fixed left-[50%] top-[50%] z-50 grid w-[calc(100%-2rem)] max-w-lg translate-x-[-50%] translate-y-[-50%] gap-4 rounded-lg border bg-background p-6 shadow-lg duration-200",
              "data-[state=open]:animate-in data-[state=closed]:animate-out",
            )}
          >
            <div className="flex flex-col gap-1.5">
              <DialogPrimitive.Title className="text-lg font-semibold leading-none tracking-tight">
                {importStep === "summary"
                  ? t("importSummaryTitle")
                  : importStep === "mapping"
                    ? t("importMappingTitle")
                    : t("importDialogTitle")}
              </DialogPrimitive.Title>
              <DialogPrimitive.Description className="text-sm text-muted-foreground">
                {importStep === "summary"
                  ? t("importSummaryDescription")
                  : importStep === "mapping"
                    ? t("importMappingDescription")
                    : t("importDialogDescription")}
              </DialogPrimitive.Description>
            </div>

            {importStep === "mapping" && mappingSessionId && (
              <div className="space-y-3 text-sm">
                <div className="max-h-[min(24rem,50vh)] space-y-2 overflow-y-auto pr-1">
                  {mappingBookingFields.map((field) => (
                    <div key={field} className="grid gap-1 sm:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)] sm:items-center">
                      <Label htmlFor={`import-map-${field}`} className="text-xs font-medium leading-tight sm:pt-0">
                        {field}
                      </Label>
                      <select
                        id={`import-map-${field}`}
                        className="flex h-9 w-full rounded-md border border-input bg-transparent px-2 py-1 text-sm shadow-sm"
                        value={columnSelections[field] ?? ""}
                        onChange={(e) =>
                          setColumnSelections((prev) => ({ ...prev, [field]: e.target.value }))
                        }
                      >
                        <option value="">{t("importMappingNoColumn")}</option>
                        {mappingFileHeaders.map((h) => (
                          <option key={h} value={h}>
                            {h}
                          </option>
                        ))}
                      </select>
                    </div>
                  ))}
                </div>
                <div className="flex flex-wrap justify-end gap-2 pt-2">
                  <DialogPrimitive.Close asChild>
                    <Button type="button" variant="outline">
                      {t("importCancel")}
                    </Button>
                  </DialogPrimitive.Close>
                  <Button type="button" disabled={importLoading} onClick={() => void handleApplyColumnMapping()}>
                    {importLoading ? t("importing") : t("importMappingContinue")}
                  </Button>
                </div>
              </div>
            )}

            {importStep === "preview" && preview && (
              <div className="space-y-4 text-sm">
                <ul className="list-inside list-disc space-y-1 text-muted-foreground">
                  <li>{t("importCountCompleted", { count: preview.completedCount })}</li>
                  <li>{t("importCountDrafts", { count: preview.draftCount })}</li>
                  <li>{t("importCountSkipped", { count: preview.skippedEmptyRows })}</li>
                  <li>{t("importCountTotal", { count: preview.totalDataRows })}</li>
                </ul>

                {preview.completedCount > 0 && (
                  <label className="flex cursor-pointer items-start gap-3 rounded-md border p-3">
                    <input
                      type="checkbox"
                      className="mt-1 size-4 shrink-0 rounded border-input"
                      checked={confirmCompleted}
                      onChange={(e) => setConfirmCompleted(e.target.checked)}
                    />
                    <span>{t("importConfirmCompleted", { count: preview.completedCount })}</span>
                  </label>
                )}
                {preview.draftCount > 0 && (
                  <label className="flex cursor-pointer items-start gap-3 rounded-md border p-3">
                    <input
                      type="checkbox"
                      className="mt-1 size-4 shrink-0 rounded border-input"
                      checked={confirmDrafts}
                      onChange={(e) => setConfirmDrafts(e.target.checked)}
                    />
                    <span>{t("importConfirmDrafts", { count: preview.draftCount })}</span>
                  </label>
                )}

                <div className="flex flex-wrap justify-end gap-2 pt-2">
                  <DialogPrimitive.Close asChild>
                    <Button type="button" variant="outline">
                      {t("importCancel")}
                    </Button>
                  </DialogPrimitive.Close>
                  <Button
                    type="button"
                    disabled={importLoading || !canSubmitImport}
                    onClick={() => void handleConfirmImport()}
                  >
                    {importLoading ? t("importing") : t("importRun")}
                  </Button>
                </div>
              </div>
            )}

            {importStep === "summary" && importResult && (
              <div className="space-y-4 text-sm">
                <p className="text-muted-foreground">
                  {t("importSummaryLine", {
                    created: importResult.createdCount,
                    drafts: importResult.draftCount,
                  })}
                </p>
                {importResult.errors.length > 0 && (
                  <div className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-destructive">
                    <p className="font-medium">{t("importErrors")}</p>
                    <ul className="mt-2 list-inside list-disc text-xs">
                      {importResult.errors.slice(0, 8).map((err) => (
                        <li key={err}>{err}</li>
                      ))}
                    </ul>
                    {importResult.errors.length > 8 && (
                      <p className="mt-1 text-xs">… (+{importResult.errors.length - 8} more)</p>
                    )}
                  </div>
                )}
                {importResult.createdBookingIds.length > 0 && (
                  <Button
                    type="button"
                    variant="secondary"
                    disabled={waybillPdfLoading}
                    onClick={() => void handleBulkWaybills()}
                  >
                    {waybillPdfLoading ? t("importWaybillsLoading") : t("importDownloadWaybills")}
                  </Button>
                )}
                <div className="flex justify-end pt-2">
                  <DialogPrimitive.Close asChild>
                    <Button type="button">{t("importDone")}</Button>
                  </DialogPrimitive.Close>
                </div>
              </div>
            )}
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>

      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        {!isSuperAdmin && (
          <div className="flex flex-wrap items-center gap-2">
            {showImportExport && (
              <>
                <Button type="button" variant="outline" disabled={importLoading} onClick={handleImportPick}>
                  {importLoading ? t("importing") : t("import")}
                </Button>
                <p className="text-xs text-muted-foreground max-w-[220px] hidden sm:block">{t("importHint")}</p>
              </>
            )}
            <Link href="/bookings/create">
              <Button>{t("createTitle")}</Button>
            </Link>
          </div>
        )}
      </div>
      <div className="flex gap-2 border-b">
        <button
          type="button"
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${tab === "completed" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
          onClick={() => setTab("completed")}
        >
          Completed
        </button>
        <button
          type="button"
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${tab === "drafts" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
          onClick={() => setTab("drafts")}
        >
          Drafts
        </button>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{isDrafts ? "Drafts" : t("title")}</CardTitle>
          <CardDescription>
            {isDrafts
              ? "Save as draft, then retrieve and fill the rest. Confirm to complete."
              : "Your completed bookings. Click a row to view details."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <p className="text-sm text-destructive mb-4" role="alert">
              {error}
            </p>
          )}
          {loading ? (
            <p className="text-muted-foreground">Loading…</p>
          ) : list.length === 0 ? (
            <>
              <p className="text-sm text-muted-foreground mb-4">
                {isDrafts ? "No drafts. Create a booking and choose “Save as draft”." : t("noBookings")}
              </p>
              {!isSuperAdmin && (
                <Link href="/bookings/create">
                  <Button variant="outline">{isDrafts ? "Create booking" : t("createFirst")}</Button>
                </Link>
              )}
            </>
          ) : (
            <BookingsDataTable
              data={list}
              isDrafts={isDrafts}
              isSuperAdmin={isSuperAdmin}
              exportLoading={exportLoading}
              onExport={handleExport}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
