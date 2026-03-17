"use client";

import { useAuth } from "@/context/AuthContext";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export default function MorePage() {
  const { user } = useAuth();
  const t = useTranslations("more");

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
            <p className="text-sm font-medium">Signed in as</p>
            <p className="text-sm text-muted-foreground">{user?.email ?? user?.displayName ?? "—"}</p>
            {user?.businessId && (
              <p className="text-sm text-muted-foreground">Company: {user.businessId}</p>
            )}
          </div>
          <p className="text-sm text-muted-foreground">
            Theme and language can be changed from the navbar. Password reset and profile settings will be available in a future update.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
