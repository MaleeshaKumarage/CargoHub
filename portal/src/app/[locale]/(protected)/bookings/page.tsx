"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import {
  bookingList,
  bookingsExportDownload,
  bookingsImport,
  draftList,
  type BookingListItem,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BookingsDataTable } from "@/components/BookingsDataTable";

type Tab = "completed" | "drafts";

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
  const [importNotice, setImportNotice] = useState<string | null>(null);
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

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !token) return;
    setImportLoading(true);
    setError(null);
    setImportNotice(null);
    try {
      const result = await bookingsImport(token, file);
      const parts = [t("importSummary", { created: result.createdCount, drafts: result.draftCount })];
      if (result.errors.length > 0) {
        parts.push(`${t("importErrors")} ${result.errors.slice(0, 5).join("; ")}`);
        if (result.errors.length > 5) parts.push(`… (+${result.errors.length - 5} more)`);
      }
      setImportNotice(parts.join(" "));
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
          {importNotice && (
            <p className="text-sm text-muted-foreground mb-4" role="status">
              {importNotice}
            </p>
          )}
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
