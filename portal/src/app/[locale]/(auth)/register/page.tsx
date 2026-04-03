"use client";

import { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { register as apiRegister } from "@/lib/api";
import { useRouter, Link } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

export default function RegisterPage() {
  const router = useRouter();
  const { login: setAuth, isAuthenticated } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [userName, setUserName] = useState("");
  const [companyId, setCompanyId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const t = useTranslations("auth");

  if (isAuthenticated) {
    router.replace("/dashboard");
    return null;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await apiRegister({
        email: email.trim(),
        password,
        userName: userName.trim(),
        businessId: companyId.trim() || undefined,
      });
      if (result.success && result.data) {
        setAuth(result.data, result.data.jwtToken);
        router.replace("/dashboard");
        return;
      }
      if (result.errorCode === "CompanyIdRequired") setError(t("companyIdRequired"));
      else if (result.errorCode === "CompanyNotFound") setError(t("companyNotFound"));
      else if (result.errorCode === "CompanyUserLimitReached") setError(t("userLimitReached"));
      else setError(result.message ?? t("registrationFailed"));
    } catch {
      setError(t("networkError"));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{t("registerTitle")}</CardTitle>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="companyId">{t("companyId")}</Label>
            <Input id="companyId" type="text" autoComplete="organization" value={companyId} onChange={(e) => setCompanyId(e.target.value)} required placeholder="e.g. 1234567-8" />
            <p className="text-xs text-muted-foreground">Government business ID. Your company must be set up by an administrator first.</p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">{t("email")}</Label>
            <Input id="email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="space-y-2">
            <Label htmlFor="userName">{t("userName")}</Label>
            <Input id="userName" type="text" autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} required />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">{t("password")}</Label>
            <Input id="password" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
          </div>
          {error && <p className="text-sm text-destructive" role="alert">{error}</p>}
        </CardContent>
        <CardFooter className="flex flex-col gap-4">
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? t("creatingAccount") : t("createAccount")}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            {t("alreadyHaveAccount")}{" "}
            <Link href="/login" className="text-primary underline-offset-4 hover:underline">
              {t("signIn")}
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
