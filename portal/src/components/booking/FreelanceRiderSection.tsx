"use client";

import { useEffect, useState } from "react";
import { getFreelanceRiderMatches, type FreelanceRiderMatch } from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { useTranslations } from "next-intl";

export type FreelanceRiderSectionProps = {
  token: string;
  shipperPostal: string;
  receiverPostal: string;
  /** Super Admin viewing a draft: pass booking header company id to scope matches */
  matchCompanyId?: string | null;
  value: string;
  onChange: (riderId: string) => void;
  disabled?: boolean;
  idPrefix?: string;
};

export function FreelanceRiderSection({
  token,
  shipperPostal,
  receiverPostal,
  matchCompanyId,
  value,
  onChange,
  disabled,
  idPrefix = "fr",
}: FreelanceRiderSectionProps) {
  const t = useTranslations("bookings.freelanceRider");
  const [matches, setMatches] = useState<FreelanceRiderMatch[]>([]);
  const [loading, setLoading] = useState(false);
  const [fetchErr, setFetchErr] = useState<string | null>(null);

  useEffect(() => {
    const sp = shipperPostal.trim();
    const rp = receiverPostal.trim();
    if (sp.length < 3 || rp.length < 3 || !token || disabled) {
      setMatches([]);
      setFetchErr(null);
      return;
    }
    let cancelled = false;
    const tmr = window.setTimeout(() => {
      setLoading(true);
      setFetchErr(null);
      getFreelanceRiderMatches(token, {
        shipperPostal: sp,
        receiverPostal: rp,
        companyId: matchCompanyId ?? undefined,
      })
        .then((list) => {
          if (!cancelled) setMatches(list);
        })
        .catch((e) => {
          if (!cancelled) setFetchErr(e instanceof Error ? e.message : t("loadFailed"));
        })
        .finally(() => {
          if (!cancelled) setLoading(false);
        });
    }, 400);
    return () => {
      cancelled = true;
      window.clearTimeout(tmr);
    };
  }, [token, shipperPostal, receiverPostal, matchCompanyId, disabled, t]);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("title")}</CardTitle>
        <CardDescription>{t("help")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-2">
        <div className="space-y-2">
          <Label htmlFor={`${idPrefix}-select`}>{t("assignLabel")}</Label>
          <select
            id={`${idPrefix}-select`}
            className="flex h-9 w-full max-w-md rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm disabled:opacity-50"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            disabled={disabled}
          >
            <option value="">{t("none")}</option>
            {matches.map((m) => (
              <option key={m.id} value={m.id}>
                {m.displayName || m.email}
                {m.displayName && m.email ? ` (${m.email})` : ""}
              </option>
            ))}
          </select>
          {loading && <p className="text-xs text-muted-foreground">{t("loadingMatches")}</p>}
          {fetchErr && (
            <p className="text-xs text-destructive" role="alert">
              {fetchErr}
            </p>
          )}
          {!loading && !fetchErr && matches.length === 0 && shipperPostal.trim().length >= 3 && receiverPostal.trim().length >= 3 && (
            <p className="text-xs text-muted-foreground">{t("noMatches")}</p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
