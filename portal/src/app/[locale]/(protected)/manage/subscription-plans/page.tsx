"use client";

import { useAuth } from "@/context/AuthContext";
import { Link, useRouter } from "@/i18n/navigation";
import {
  adminCreateSubscriptionPlan,
  adminGetSubscriptionPlans,
  type AdminSubscriptionPlanSummary,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog as DialogPrimitive } from "radix-ui";
import { cn } from "@/lib/utils";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

const planKinds = [
  "Trial",
  "PayPerBooking",
  "TieredPayPerBooking",
  "MonthlyBundle",
  "TieredMonthlyByUsage",
] as const;

const anchors = ["FirstBillableAtUtc", "CreatedAtUtc"] as const;

export default function ManageSubscriptionPlansPage() {
  const { token } = useAuth();
  const router = useRouter();
  const t = useTranslations("manageSubscriptionPlans");
  const tInv = useTranslations("manageInvoices");
  const [plans, setPlans] = useState<AdminSubscriptionPlanSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [creating, setCreating] = useState(false);
  const [formName, setFormName] = useState("");
  const [formKind, setFormKind] = useState<string>(planKinds[0]);
  const [formAnchor, setFormAnchor] = useState<string>(anchors[0]);
  const [formTrial, setFormTrial] = useState("5");
  const [formCurrency, setFormCurrency] = useState("EUR");
  const [formActive, setFormActive] = useState(true);

  const reload = () => {
    if (!token) return;
    setLoading(true);
    adminGetSubscriptionPlans(token)
      .then(setPlans)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (!token) return;
    reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- reload on token only
  }, [token]);

  const onCreate = async () => {
    if (!token) return;
    setCreating(true);
    setError(null);
    try {
      const trial =
        formKind === "Trial" ? Math.max(1, Number.parseInt(formTrial, 10) || 1) : undefined;
      const { id } = await adminCreateSubscriptionPlan(token, {
        name: formName.trim(),
        kind: formKind,
        chargeTimeAnchor: formAnchor,
        trialBookingAllowance: trial,
        currency: formCurrency.trim().toUpperCase() || "EUR",
        isActive: formActive,
      });
      setCreateOpen(false);
      setFormName("");
      router.push(`/manage/subscription-plans/${id}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Create failed");
    } finally {
      setCreating(false);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("title")}</h1>
          <p className="text-muted-foreground mt-1">{t("description")}</p>
        </div>
        <Button type="button" onClick={() => setCreateOpen(true)}>
          {t("addPlan")}
        </Button>
      </div>

      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>{t("listHeading")}</CardTitle>
          <CardDescription>{t("description")}</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <p className="text-sm text-muted-foreground">{t("loading")}</p>
          ) : plans.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("addPlan")}</p>
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left font-medium">{t("name")}</th>
                    <th className="p-3 text-left font-medium">{t("kind")}</th>
                    <th className="p-3 text-left font-medium">{t("currency")}</th>
                    <th className="p-3 text-left font-medium">{t("active")}</th>
                    <th className="p-3 text-left font-medium" />
                  </tr>
                </thead>
                <tbody>
                  {plans.map((p) => (
                    <tr key={p.id} className="border-b last:border-0">
                      <td className="p-3">{p.name}</td>
                      <td className="p-3 font-mono text-xs">{p.kind}</td>
                      <td className="p-3">{p.currency}</td>
                      <td className="p-3">{p.isActive ? "✓" : "—"}</td>
                      <td className="p-3">
                        <Button type="button" variant="secondary" size="sm" asChild>
                          <Link href={`/manage/subscription-plans/${p.id}`}>{t("openEdit")}</Link>
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

      <DialogPrimitive.Root open={createOpen} onOpenChange={setCreateOpen}>
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
          <DialogPrimitive.Content
            className={cn(
              "data-[state=open]:animate-in data-[state=closed]:animate-out fixed left-[50%] top-[50%] z-50 w-full max-w-lg translate-x-[-50%] translate-y-[-50%] rounded-lg border bg-background p-6 shadow-lg",
            )}
          >
            <DialogPrimitive.Title className="text-lg font-semibold">{t("createTitle")}</DialogPrimitive.Title>
            <div className="mt-4 space-y-3">
              <div className="space-y-1">
                <Label htmlFor="np-name">{t("name")}</Label>
                <Input id="np-name" value={formName} onChange={(e) => setFormName(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label htmlFor="np-kind">{t("kind")}</Label>
                <select
                  id="np-kind"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={formKind}
                  onChange={(e) => setFormKind(e.target.value)}
                >
                  {planKinds.map((k) => (
                    <option key={k} value={k}>
                      {k}
                    </option>
                  ))}
                </select>
              </div>
              <div className="space-y-1">
                <Label htmlFor="np-anchor">{t("chargeAnchor")}</Label>
                <select
                  id="np-anchor"
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs"
                  value={formAnchor}
                  onChange={(e) => setFormAnchor(e.target.value)}
                >
                  {anchors.map((a) => (
                    <option key={a} value={a}>
                      {a}
                    </option>
                  ))}
                </select>
              </div>
              {formKind === "Trial" && (
                <div className="space-y-1">
                  <Label htmlFor="np-trial">{t("trialAllowance")}</Label>
                  <Input
                    id="np-trial"
                    type="number"
                    min={1}
                    value={formTrial}
                    onChange={(e) => setFormTrial(e.target.value)}
                  />
                </div>
              )}
              <div className="space-y-1">
                <Label htmlFor="np-cur">{t("currency")}</Label>
                <Input
                  id="np-cur"
                  maxLength={3}
                  value={formCurrency}
                  onChange={(e) => setFormCurrency(e.target.value)}
                />
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={formActive} onChange={(e) => setFormActive(e.target.checked)} />
                {t("active")}
              </label>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <DialogPrimitive.Close asChild>
                <Button type="button" variant="outline">
                  {tInv("close")}
                </Button>
              </DialogPrimitive.Close>
              <Button type="button" disabled={creating || !formName.trim()} onClick={onCreate}>
                {creating ? "…" : t("savePlan")}
              </Button>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </div>
  );
}
