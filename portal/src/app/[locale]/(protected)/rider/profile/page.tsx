"use client";

import { useAuth } from "@/context/AuthContext";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { riderGetMe, riderPatchMe } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

export default function RiderProfilePage() {
  const { token, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("rider");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [postals, setPostals] = useState("");
  const [businessId, setBusinessId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) router.replace("/login");
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (!token) return;
    setLoading(true);
    setError(null);
    riderGetMe(token)
      .then((me) => {
        setBusinessId(me.businessId);
        setDisplayName(me.displayName);
        setPhone(me.phone);
        setEmail(me.email);
        setPostals((me.postalCodes ?? []).join("\n"));
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Failed"))
      .finally(() => setLoading(false));
  }, [token]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      await riderPatchMe(token, {
        phone: phone.trim() || undefined,
        email: email.trim() || undefined,
        postalCodes: postals
          .split(/[\s,;]+/g)
          .map((x) => x.trim())
          .filter(Boolean),
      });
      setSaved(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed");
    } finally {
      setSaving(false);
    }
  }

  if (!isAuthenticated || isLoading) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("profileTitle")}</h1>
        <p className="text-muted-foreground mt-1">{t("profileDescription")}</p>
      </div>
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}
      {saved ? (
        <p className="text-sm text-green-600 dark:text-green-500" role="status">
          {t("saved")}
        </p>
      ) : null}
      <Card>
        <CardHeader>
          <CardTitle>{t("profileTitle")}</CardTitle>
          <CardDescription>{t("readOnlyBusinessId")}</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <p className="text-muted-foreground">Loading…</p>
          ) : (
            <form onSubmit={onSubmit} className="max-w-md space-y-4">
              <div className="space-y-2">
                <Label htmlFor="rb">{t("readOnlyBusinessId")}</Label>
                <Input id="rb" value={businessId} readOnly className="bg-muted" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="rd">{t("readOnlyName")}</Label>
                <Input id="rd" value={displayName} readOnly className="bg-muted" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="rp">{t("phone")}</Label>
                <Input id="rp" value={phone} onChange={(e) => setPhone(e.target.value)} autoComplete="tel" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="re">{t("email")}</Label>
                <Input id="re" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="rpc">{t("postalCodes")}</Label>
                <textarea
                  id="rpc"
                  rows={5}
                  value={postals}
                  onChange={(e) => setPostals(e.target.value)}
                  className={cn(
                    "flex min-h-[100px] w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm",
                    "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
                  )}
                />
              </div>
              <Button type="submit" disabled={saving}>
                {saving ? "…" : "Save"}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
