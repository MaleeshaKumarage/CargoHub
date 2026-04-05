"use client";

import { useAuth } from "@/context/AuthContext";
import { Link, useRouter } from "@/i18n/navigation";
import { useParams } from "next/navigation";
import {
  adminAddSubscriptionPlanPricingPeriod,
  adminDeleteSubscriptionPlan,
  adminDeleteSubscriptionPlanPricingPeriod,
  adminGetSubscriptionPlanDetail,
  adminReplaceSubscriptionPlanPricingTiers,
  adminUpdateSubscriptionPlan,
  adminUpdateSubscriptionPlanPricingPeriod,
  type AdminPricingPeriodDetail,
  type AdminSubscriptionPlanDetail,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useTranslations } from "next-intl";
import { useCallback, useEffect, useMemo, useState } from "react";

const planKinds = [
  "Trial",
  "PayPerBooking",
  "TieredPayPerBooking",
  "MonthlyBundle",
  "TieredMonthlyByUsage",
] as const;

const anchors = ["FirstBillableAtUtc", "CreatedAtUtc"] as const;

function isoUtcToDatetimeLocalValue(iso: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(iso);
  if (!m) return "";
  return `${m[1]}-${m[2]}-${m[3]}T${m[4]}:${m[5]}`;
}

function datetimeLocalUtcToIso(s: string): string {
  const [date, time] = s.split("T");
  if (!date || !time) return new Date().toISOString();
  const [y, mo, d] = date.split("-").map((x) => Number(x));
  const [hh, mm] = time.split(":").map((x) => Number(x));
  return new Date(Date.UTC(y, mo - 1, d, hh, mm, 0)).toISOString();
}

function optDecimal(s: string): number | null {
  const t = s.trim();
  if (t === "") return null;
  const n = Number(t);
  return Number.isFinite(n) ? n : null;
}

function optInt(s: string): number | null {
  const t = s.trim();
  if (t === "") return null;
  const n = Number.parseInt(t, 10);
  return Number.isFinite(n) ? n : null;
}

type TierRow = { clientId: string; id?: string; ordinal: string; max: string; cpb: string; mf: string };

function tiersFromDetail(period: AdminPricingPeriodDetail): TierRow[] {
  if (period.tiers.length === 0)
    return [{ clientId: crypto.randomUUID(), ordinal: "1", max: "", cpb: "", mf: "" }];
  return period.tiers.map((t) => ({
    clientId: crypto.randomUUID(),
    id: t.id,
    ordinal: String(t.ordinal),
    max: t.inclusiveMaxBookingsInPeriod != null ? String(t.inclusiveMaxBookingsInPeriod) : "",
    cpb: t.chargePerBooking != null ? String(t.chargePerBooking) : "",
    mf: t.monthlyFee != null ? String(t.monthlyFee) : "",
  }));
}

export default function EditSubscriptionPlanPage() {
  const params = useParams();
  const planId = String(params?.planId ?? "");
  const { token } = useAuth();
  const router = useRouter();
  const t = useTranslations("manageSubscriptionPlans");

  const [plan, setPlan] = useState<AdminSubscriptionPlanDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [kind, setKind] = useState<string>(planKinds[0]);
  const [chargeAnchor, setChargeAnchor] = useState<string>(anchors[0]);
  const [trial, setTrial] = useState("");
  const [currency, setCurrency] = useState("EUR");
  const [isActive, setIsActive] = useState(true);
  const [savingPlan, setSavingPlan] = useState(false);

  const [newEff, setNewEff] = useState("");
  const [newCpb, setNewCpb] = useState("");
  const [newMf, setNewMf] = useState("");
  const [newInc, setNewInc] = useState("");
  const [newOv, setNewOv] = useState("");
  const [adding, setAdding] = useState(false);

  const [periodForms, setPeriodForms] = useState<
    Record<
      string,
      { eff: string; cpb: string; mf: string; inc: string; ov: string; saving: boolean; tierRows: TierRow[] }
    >
  >({});

  const load = useCallback(() => {
    if (!token || !planId) return;
    setLoading(true);
    adminGetSubscriptionPlanDetail(token, planId)
      .then((p) => {
        setPlan(p);
        setName(p.name);
        setKind(p.kind);
        setChargeAnchor(p.chargeTimeAnchor);
        setTrial(p.trialBookingAllowance != null ? String(p.trialBookingAllowance) : "");
        setCurrency(p.currency);
        setIsActive(p.isActive);
        const next: typeof periodForms = {};
        for (const pp of p.pricingPeriods) {
          next[pp.id] = {
            eff: isoUtcToDatetimeLocalValue(pp.effectiveFromUtc),
            cpb: pp.chargePerBooking != null ? String(pp.chargePerBooking) : "",
            mf: pp.monthlyFee != null ? String(pp.monthlyFee) : "",
            inc: pp.includedBookingsPerMonth != null ? String(pp.includedBookingsPerMonth) : "",
            ov: pp.overageChargePerBooking != null ? String(pp.overageChargePerBooking) : "",
            saving: false,
            tierRows: tiersFromDetail(pp),
          };
        }
        setPeriodForms(next);
        setError(null);
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, planId]);

  useEffect(() => {
    load();
  }, [load]);

  const sortedPeriods = useMemo(() => {
    if (!plan) return [];
    return [...plan.pricingPeriods].sort(
      (a, b) => new Date(b.effectiveFromUtc).getTime() - new Date(a.effectiveFromUtc).getTime()
    );
  }, [plan]);

  const savePlan = async () => {
    if (!token || !plan) return;
    setSavingPlan(true);
    setError(null);
    try {
      const trialNum =
        kind === "Trial" ? Math.max(1, Number.parseInt(trial, 10) || 1) : null;
      await adminUpdateSubscriptionPlan(token, plan.id, {
        name: name.trim(),
        kind,
        chargeTimeAnchor: chargeAnchor,
        trialBookingAllowance: trialNum,
        currency: currency.trim().toUpperCase() || "EUR",
        isActive,
      });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Save failed");
    } finally {
      setSavingPlan(false);
    }
  };

  const deletePlan = async () => {
    if (!token || !plan) return;
    if (!window.confirm(t("confirmDelete"))) return;
    setError(null);
    try {
      await adminDeleteSubscriptionPlan(token, plan.id);
      router.push("/manage/subscription-plans");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Delete failed");
    }
  };

  const savePeriod = async (periodId: string) => {
    if (!token) return;
    const f = periodForms[periodId];
    if (!f) return;
    setPeriodForms((prev) => ({
      ...prev,
      [periodId]: { ...f, saving: true },
    }));
    setError(null);
    try {
      await adminUpdateSubscriptionPlanPricingPeriod(token, periodId, {
        effectiveFromUtc: datetimeLocalUtcToIso(f.eff),
        chargePerBooking: optDecimal(f.cpb),
        monthlyFee: optDecimal(f.mf),
        includedBookingsPerMonth: optInt(f.inc),
        overageChargePerBooking: optDecimal(f.ov),
      });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Period save failed");
    } finally {
      setPeriodForms((prev) => {
        const cur = prev[periodId];
        return cur ? { ...prev, [periodId]: { ...cur, saving: false } } : prev;
      });
    }
  };

  const deletePeriod = async (periodId: string) => {
    if (!token) return;
    if (!window.confirm(t("deletePeriod") + "?")) return;
    setError(null);
    try {
      await adminDeleteSubscriptionPlanPricingPeriod(token, periodId);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Delete period failed");
    }
  };

  const saveTiers = async (periodId: string) => {
    if (!token) return;
    const f = periodForms[periodId];
    if (!f) return;
    const tiers = f.tierRows.map((row) => ({
      id: row.id ?? null,
      ordinal: Number.parseInt(row.ordinal, 10) || 0,
      inclusiveMaxBookingsInPeriod: optInt(row.max),
      chargePerBooking: optDecimal(row.cpb),
      monthlyFee: optDecimal(row.mf),
    }));
    setError(null);
    try {
      await adminReplaceSubscriptionPlanPricingTiers(token, periodId, tiers);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Tiers save failed");
    }
  };

  const addPeriod = async () => {
    if (!token || !plan) return;
    if (!newEff) {
      setError("Effective from is required.");
      return;
    }
    setAdding(true);
    setError(null);
    try {
      await adminAddSubscriptionPlanPricingPeriod(token, plan.id, {
        effectiveFromUtc: datetimeLocalUtcToIso(newEff),
        chargePerBooking: optDecimal(newCpb),
        monthlyFee: optDecimal(newMf),
        includedBookingsPerMonth: optInt(newInc),
        overageChargePerBooking: optDecimal(newOv),
      });
      setNewEff("");
      setNewCpb("");
      setNewMf("");
      setNewInc("");
      setNewOv("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Add period failed");
    } finally {
      setAdding(false);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3">
        <Button type="button" variant="outline" size="sm" asChild>
          <Link href="/manage/subscription-plans">{t("backToList")}</Link>
        </Button>
      </div>

      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground mt-1 font-mono text-sm">{planId}</p>
      </div>

      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}

      {loading || !plan ? (
        <p className="text-sm text-muted-foreground">{t("loading")}</p>
      ) : (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t("savePlan")}</CardTitle>
              <CardDescription>{t("description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 max-w-xl">
              <div className="space-y-1">
                <Label htmlFor="pl-name">{t("name")}</Label>
                <Input id="pl-name" value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="pl-kind">{t("kind")}</Label>
                <select
                  id="pl-kind"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={kind}
                  onChange={(e) => setKind(e.target.value)}
                >
                  {planKinds.map((k) => (
                    <option key={k} value={k}>
                      {k}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="pl-anchor">{t("chargeAnchor")}</Label>
                <select
                  id="pl-anchor"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={chargeAnchor}
                  onChange={(e) => setChargeAnchor(e.target.value)}
                >
                  {anchors.map((a) => (
                    <option key={a} value={a}>
                      {a}
                    </option>
                  ))}
                </select>
              </div>
              {kind === "Trial" && (
                <div className="space-y-1">
                  <Label htmlFor="pl-trial">{t("trialAllowance")}</Label>
                  <Input id="pl-trial" type="number" min={1} value={trial} onChange={(e) => setTrial(e.target.value)} />
                </div>
              )}
              <div className="space-y-1">
                <Label htmlFor="pl-cur">{t("currency")}</Label>
                <Input id="pl-cur" maxLength={3} value={currency} onChange={(e) => setCurrency(e.target.value)} />
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
                {t("active")}
              </label>
              <div className="flex flex-wrap gap-2 pt-2">
                <Button type="button" disabled={savingPlan} onClick={savePlan}>
                  {savingPlan ? "…" : t("savePlan")}
                </Button>
                <Button type="button" variant="destructive" onClick={deletePlan}>
                  {t("deletePlan")}
                </Button>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("periodsHeading")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="rounded-md border p-4 space-y-2">
                <p className="text-sm font-medium">{t("addPeriod")}</p>
                <div className="grid gap-2 sm:grid-cols-2">
                  <div className="space-y-1">
                    <Label>{t("effectiveFrom")}</Label>
                    <Input type="datetime-local" value={newEff} onChange={(e) => setNewEff(e.target.value)} />
                  </div>
                  <div className="space-y-1">
                    <Label>{t("chargePerBooking")}</Label>
                    <Input value={newCpb} onChange={(e) => setNewCpb(e.target.value)} />
                  </div>
                  <div className="space-y-1">
                    <Label>{t("monthlyFee")}</Label>
                    <Input value={newMf} onChange={(e) => setNewMf(e.target.value)} />
                  </div>
                  <div className="space-y-1">
                    <Label>{t("includedBookings")}</Label>
                    <Input value={newInc} onChange={(e) => setNewInc(e.target.value)} />
                  </div>
                  <div className="space-y-1">
                    <Label>{t("overage")}</Label>
                    <Input value={newOv} onChange={(e) => setNewOv(e.target.value)} />
                  </div>
                </div>
                <Button type="button" size="sm" disabled={adding} onClick={addPeriod}>
                  {adding ? "…" : t("addPeriod")}
                </Button>
              </div>

              {sortedPeriods.map((pp) => {
                const f = periodForms[pp.id];
                if (!f) return null;
                return (
                  <div key={pp.id} className="rounded-md border p-4 space-y-4">
                    <p className="text-xs font-mono text-muted-foreground">{pp.id}</p>
                    <div className="grid gap-2 sm:grid-cols-2">
                      <div className="space-y-1">
                        <Label>{t("effectiveFrom")}</Label>
                        <Input
                          type="datetime-local"
                          value={f.eff}
                          onChange={(e) =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              return { ...prev, [pp.id]: { ...cur, eff: e.target.value } };
                            })
                          }
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>{t("chargePerBooking")}</Label>
                        <Input
                          value={f.cpb}
                          onChange={(e) =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              return { ...prev, [pp.id]: { ...cur, cpb: e.target.value } };
                            })
                          }
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>{t("monthlyFee")}</Label>
                        <Input
                          value={f.mf}
                          onChange={(e) =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              return { ...prev, [pp.id]: { ...cur, mf: e.target.value } };
                            })
                          }
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>{t("includedBookings")}</Label>
                        <Input
                          value={f.inc}
                          onChange={(e) =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              return { ...prev, [pp.id]: { ...cur, inc: e.target.value } };
                            })
                          }
                        />
                      </div>
                      <div className="space-y-1">
                        <Label>{t("overage")}</Label>
                        <Input
                          value={f.ov}
                          onChange={(e) =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              return { ...prev, [pp.id]: { ...cur, ov: e.target.value } };
                            })
                          }
                        />
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <Button type="button" size="sm" disabled={f.saving} onClick={() => savePeriod(pp.id)}>
                        {f.saving ? "…" : t("savePeriod")}
                      </Button>
                      <Button type="button" size="sm" variant="destructive" onClick={() => deletePeriod(pp.id)}>
                        {t("deletePeriod")}
                      </Button>
                    </div>

                    <div className="space-y-2">
                      <p className="text-sm font-medium">{t("tiersHeading")}</p>
                      <div className="overflow-x-auto rounded border">
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="border-b bg-muted/40">
                              <th className="p-2 text-left">{t("tierOrdinal")}</th>
                              <th className="p-2 text-left">{t("tierMax")}</th>
                              <th className="p-2 text-left">{t("chargePerBooking")}</th>
                              <th className="p-2 text-left">{t("monthlyFee")}</th>
                              <th className="p-2 w-8" />
                            </tr>
                          </thead>
                          <tbody>
                            {f.tierRows.map((row) => (
                              <tr key={row.clientId} className="border-b last:border-0">
                                <td className="p-1">
                                  <Input
                                    className="h-8"
                                    value={row.ordinal}
                                    onChange={(e) =>
                                      setPeriodForms((prev) => {
                                        const cur = prev[pp.id];
                                        if (!cur) return prev;
                                        return {
                                          ...prev,
                                          [pp.id]: {
                                            ...cur,
                                            tierRows: cur.tierRows.map((r) =>
                                              r.clientId === row.clientId ? { ...r, ordinal: e.target.value } : r
                                            ),
                                          },
                                        };
                                      })
                                    }
                                  />
                                </td>
                                <td className="p-1">
                                  <Input
                                    className="h-8"
                                    value={row.max}
                                    onChange={(e) =>
                                      setPeriodForms((prev) => ({
                                        ...prev,
                                        [pp.id]: {
                                          ...f,
                                          tierRows: f.tierRows.map((r) =>
                                            r.clientId === row.clientId ? { ...r, max: e.target.value } : r
                                          ),
                                        },
                                      }))
                                    }
                                  />
                                </td>
                                <td className="p-1">
                                  <Input
                                    className="h-8"
                                    value={row.cpb}
                                    onChange={(e) =>
                                      setPeriodForms((prev) => {
                                        const cur = prev[pp.id];
                                        if (!cur) return prev;
                                        return {
                                          ...prev,
                                          [pp.id]: {
                                            ...cur,
                                            tierRows: cur.tierRows.map((r) =>
                                              r.clientId === row.clientId ? { ...r, cpb: e.target.value } : r
                                            ),
                                          },
                                        };
                                      })
                                    }
                                  />
                                </td>
                                <td className="p-1">
                                  <Input
                                    className="h-8"
                                    value={row.mf}
                                    onChange={(e) =>
                                      setPeriodForms((prev) => {
                                        const cur = prev[pp.id];
                                        if (!cur) return prev;
                                        return {
                                          ...prev,
                                          [pp.id]: {
                                            ...cur,
                                            tierRows: cur.tierRows.map((r) =>
                                              r.clientId === row.clientId ? { ...r, mf: e.target.value } : r
                                            ),
                                          },
                                        };
                                      })
                                    }
                                  />
                                </td>
                                <td className="p-1">
                                  <Button
                                    type="button"
                                    variant="ghost"
                                    size="sm"
                                    className="h-8 px-2"
                                    onClick={() =>
                                      setPeriodForms((prev) => {
                                        const cur = prev[pp.id];
                                        if (!cur) return prev;
                                        return {
                                          ...prev,
                                          [pp.id]: {
                                            ...cur,
                                            tierRows: cur.tierRows.filter((r) => r.clientId !== row.clientId),
                                          },
                                        };
                                      })
                                    }
                                  >
                                    ×
                                  </Button>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="secondary"
                          onClick={() =>
                            setPeriodForms((prev) => {
                              const cur = prev[pp.id];
                              if (!cur) return prev;
                              const nextOrd =
                                Math.max(0, ...cur.tierRows.map((r) => Number.parseInt(r.ordinal, 10) || 0)) + 1;
                              return {
                                ...prev,
                                [pp.id]: {
                                  ...cur,
                                  tierRows: [
                                    ...cur.tierRows,
                                    {
                                      clientId: crypto.randomUUID(),
                                      ordinal: String(nextOrd),
                                      max: "",
                                      cpb: "",
                                      mf: "",
                                    },
                                  ],
                                },
                              };
                            })
                          }
                        >
                          {t("addTierRow")}
                        </Button>
                        <Button type="button" size="sm" onClick={() => saveTiers(pp.id)}>
                          {t("saveTiers")}
                        </Button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
