"use client";

import { useAuth } from "@/context/AuthContext";
import { Link, useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getCourierCatalog,
  getCourierContracts,
  putCourierContracts,
  type CourierContractInput,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type Row = { courierId: string; contractId: string; service: string };

export default function CourierContractsPage() {
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("courierContracts");
  const [rows, setRows] = useState<Row[]>([]);
  const [catalog, setCatalog] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const roles = user?.roles ?? [];
  const isAdmin = Array.isArray(roles) && roles.includes("Admin");
  const isSuperAdmin = Array.isArray(roles) && roles.includes("SuperAdmin");

  const loadContracts = useCallback(async () => {
    if (!token) return;
    setError(null);
    const contracts = await getCourierContracts(token);
    setRows(
      contracts.map((c) => ({
        courierId: c.courierId,
        contractId: c.contractId,
        service: c.service?.trim() ? c.service : "",
      }))
    );
  }, [token]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) router.replace("/login");
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (!token || isSuperAdmin) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    (async () => {
      try {
        await loadContracts();
        if (cancelled) return;
        if (isAdmin) {
          try {
            const ids = await getCourierCatalog(token);
            if (!cancelled) setCatalog(ids);
          } catch {
            if (!cancelled) setCatalog([]);
          }
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : t("loadFailed"));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [token, isSuperAdmin, isAdmin, loadContracts, t]);

  const canAddCourier = useMemo(() => {
    if (!isAdmin || catalog.length === 0) return false;
    const used = new Set(rows.map((r) => r.courierId.trim().toLowerCase()).filter(Boolean));
    return catalog.some((id) => !used.has(id.toLowerCase()));
  }, [isAdmin, catalog, rows]);

  function courierOptionsForRow(index: number): string[] {
    const current = rows[index]?.courierId?.trim() ?? "";
    const otherUsed = new Set(
      rows
        .map((r, i) => (i === index ? null : r.courierId.trim().toLowerCase()))
        .filter((x): x is string => !!x)
    );
    return catalog.filter(
      (id) =>
        id.toLowerCase() === current.toLowerCase() || !otherUsed.has(id.toLowerCase())
    );
  }

  function addRow() {
    const used = new Set(rows.map((r) => r.courierId.trim().toLowerCase()).filter(Boolean));
    const next = catalog.find((id) => !used.has(id.toLowerCase())) ?? "";
    setRows((r) => [...r, { courierId: next, contractId: "", service: "" }]);
  }

  function removeRow(index: number) {
    setRows((r) => r.filter((_, i) => i !== index));
  }

  function updateRow(index: number, patch: Partial<Row>) {
    setRows((r) => r.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  }

  async function save() {
    if (!token) return;
    setSaving(true);
    setError(null);
    setMessage(null);
    try {
      const payload: CourierContractInput[] = rows.map((r) => ({
        courierId: r.courierId.trim(),
        contractId: r.contractId.trim(),
        service: r.service.trim() || undefined,
      }));
      await putCourierContracts(token, payload);
      setMessage(t("saved"));
      await loadContracts();
    } catch (e) {
      setError(e instanceof Error ? e.message : t("saveFailed"));
    } finally {
      setSaving(false);
    }
  }

  if (!isLoading && !isAuthenticated) return null;

  if (isSuperAdmin) {
    return (
      <div className="mx-auto max-w-lg space-y-4">
        <p className="text-muted-foreground">{t("superAdminNoAccess")}</p>
        <Link href="/dashboard">
          <Button variant="outline">{t("backDashboard")}</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/dashboard">
          <Button variant="ghost">{t("back")}</Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{t("title")}</CardTitle>
          <CardDescription>{t("description")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {loading && <p className="text-sm text-muted-foreground">{t("loading")}</p>}
          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}
          {message && (
            <p className="text-sm text-green-600 dark:text-green-500" role="status">
              {message}
            </p>
          )}
          {!isAdmin && !loading && (
            <p className="text-sm text-muted-foreground">{t("readOnlyHint")}</p>
          )}
          {isAdmin && !loading && catalog.length === 0 && (
            <p className="text-sm text-destructive">{t("catalogUnavailable")}</p>
          )}
          {!loading && rows.length === 0 && (
            <p className="text-sm text-muted-foreground">{t("emptyList")}</p>
          )}
          {!loading &&
            rows.map((row, index) => {
              const opts = isAdmin ? courierOptionsForRow(index) : [row.courierId].filter(Boolean);
              return (
                <div key={index} className="space-y-3 rounded-lg border p-4">
                  <div className="grid gap-3 sm:grid-cols-3">
                    <div className="space-y-1">
                      <Label>{t("courier")}</Label>
                      {isAdmin ? (
                        <select
                          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                          value={row.courierId}
                          onChange={(e) => updateRow(index, { courierId: e.target.value })}
                        >
                          {opts.length === 0 ? (
                            <option value="">{t("selectCourier")}</option>
                          ) : (
                            opts.map((id) => (
                              <option key={id} value={id}>
                                {id}
                              </option>
                            ))
                          )}
                        </select>
                      ) : (
                        <p className="text-sm font-medium">{row.courierId || "—"}</p>
                      )}
                    </div>
                    <div className="space-y-1">
                      <Label>{t("contractId")}</Label>
                      {isAdmin ? (
                        <Input
                          value={row.contractId}
                          onChange={(e) => updateRow(index, { contractId: e.target.value })}
                          placeholder={t("contractIdPlaceholder")}
                        />
                      ) : (
                        <p className="text-sm">{row.contractId || "—"}</p>
                      )}
                    </div>
                    <div className="space-y-1">
                      <Label>{t("serviceOptional")}</Label>
                      {isAdmin ? (
                        <Input
                          value={row.service}
                          onChange={(e) => updateRow(index, { service: e.target.value })}
                          placeholder={t("servicePlaceholder")}
                        />
                      ) : (
                        <p className="text-sm">{row.service || "—"}</p>
                      )}
                    </div>
                  </div>
                  {isAdmin && (
                    <Button type="button" variant="outline" size="sm" onClick={() => removeRow(index)}>
                      {t("remove")}
                    </Button>
                  )}
                </div>
              );
            })}
          {isAdmin && !loading && (
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="secondary" onClick={addRow} disabled={!canAddCourier}>
                {t("addCourier")}
              </Button>
              <Button type="button" onClick={save} disabled={saving}>
                {saving ? t("saving") : t("save")}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
