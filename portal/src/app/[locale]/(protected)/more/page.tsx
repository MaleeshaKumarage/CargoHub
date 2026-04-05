"use client";

import { useAuth } from "@/context/AuthContext";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { getCompanySubscription, type PortalCompanySubscriptionDto } from "@/lib/api";
import { buildSubscriptionPricingLines } from "@/lib/subscription-pricing";

export default function MorePage() {
  const { user, token } = useAuth();
  const t = useTranslations("more");
  const roles = user?.roles ?? [];
  const isSuperAdmin = Array.isArray(roles) && roles.includes("SuperAdmin");
  const [subscription, setSubscription] = useState<PortalCompanySubscriptionDto | null>(null);
  const [subscriptionError, setSubscriptionError] = useState(false);

  useEffect(() => {
    if (!token || !user?.businessId) {
      setSubscription(null);
      setSubscriptionError(false);
      return;
    }
    let cancelled = false;
    setSubscriptionError(false);
    getCompanySubscription(token)
      .then((d) => {
        if (!cancelled) setSubscription(d);
      })
      .catch(() => {
        if (!cancelled) {
          setSubscription(null);
          setSubscriptionError(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [token, user?.businessId]);

  const pricingLines =
    subscription != null
      ? buildSubscriptionPricingLines(subscription, (key, values) => {
          // All keys exist under `more` in messages; cast avoids strict key union drift.
          // eslint-disable-next-line @typescript-eslint/no-explicit-any -- dynamic keys from buildSubscriptionPricingLines
          return (t as any)(key, values);
        })
      : [];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{t("title")}</CardTitle>
          <CardDescription>{t("description")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="rounded-lg border p-4 space-y-1">
            <p className="text-sm font-medium">{t("signedInAs")}</p>
            <p className="text-sm text-muted-foreground">{user?.email ?? user?.displayName ?? "—"}</p>
            {user?.businessId && (
              <p className="text-sm text-muted-foreground">
                {t("companyLabel")}: {user.businessId}
              </p>
            )}
            {user?.businessId && (subscription != null || subscriptionError) && (
              <div className="mt-4 space-y-3 border-t border-border pt-4">
                <p className="text-sm font-medium">{t("subscriptionTitle")}</p>
                {subscriptionError ? (
                  <p className="text-sm text-muted-foreground">{t("subscriptionLoadError")}</p>
                ) : subscription != null ? (
                  <>
                    <div className="space-y-1">
                      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t("subscriptionPlanLabel")}
                      </p>
                      <p className="text-sm text-foreground">
                        {subscription.planName?.trim() ? subscription.planName : t("subscriptionUnnamedPlan")}
                      </p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                        {t("subscriptionPricingLabel")}
                      </p>
                      <ul className="list-inside list-disc space-y-1 text-sm text-muted-foreground">
                        {pricingLines.map((line) => (
                          <li key={line}>{line}</li>
                        ))}
                      </ul>
                    </div>
                  </>
                ) : null}
              </div>
            )}
          </div>
          {!isSuperAdmin && (
            <div className="rounded-lg border p-4 space-y-2">
              <p className="text-sm font-medium">{t("courierContracts")}</p>
              <p className="text-sm text-muted-foreground">{t("courierContractsHint")}</p>
              <Link href="/company/courier-contracts">
                <Button variant="outline" size="sm">
                  {t("courierContracts")}
                </Button>
              </Link>
            </div>
          )}
          <p className="text-sm text-muted-foreground">
            Theme and language can be changed from the navbar. Password reset and profile settings will be available in a future update.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
