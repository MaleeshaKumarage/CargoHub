"use client";

import { Suspense, useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { acceptCompanyAdminInvite, acceptFreelanceRiderInvite } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";
import { Link, useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

function AcceptInviteForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login: setAuth, isAuthenticated } = useAuth();
  const [token, setToken] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const t = useTranslations("auth");

  const inviteKind = searchParams.get("type") === "rider" ? "rider" : "admin";

  useEffect(() => {
    const q = searchParams.get("token");
    if (q) setToken(q);
  }, [searchParams]);

  if (isAuthenticated) {
    router.replace(inviteKind === "rider" ? "/rider/deliveries" : "/dashboard");
    return null;
  }

  const hasToken = token.trim().length > 0;

  function mapInviteError(errorCode: string | null | undefined): string {
    if (errorCode === "InviteTokenRequired") return t("inviteMissingLink");
    return t("inviteInvalid");
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!hasToken) {
      setError(t("inviteMissingLink"));
      return;
    }
    if (password !== confirmPassword) {
      setError(t("passwordMismatch"));
      return;
    }
    setLoading(true);
    try {
      const result =
        inviteKind === "rider"
          ? await acceptFreelanceRiderInvite({
              token: token.trim(),
              password,
              userName: userName.trim(),
            })
          : await acceptCompanyAdminInvite({
              token: token.trim(),
              password,
              userName: userName.trim(),
            });
      if (result.success && result.data) {
        setAuth(result.data, result.data.jwtToken);
        router.replace(inviteKind === "rider" ? "/rider/deliveries" : "/dashboard");
        return;
      }
      setError(result.message ?? mapInviteError(result.errorCode));
    } catch {
      setError(t("inviteInvalid"));
    } finally {
      setLoading(false);
    }
  }

  if (!hasToken) {
    return (
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>{t("inviteTitle")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground" role="alert">
            {t("inviteMissingLink")}
          </p>
        </CardContent>
        <CardFooter>
          <Link
            href="/login"
            className="text-center text-sm text-muted-foreground underline-offset-4 hover:underline"
          >
            {t("backToSignIn")}
          </Link>
        </CardFooter>
      </Card>
    );
  }

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{t("inviteTitle")}</CardTitle>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <p className="text-xs text-muted-foreground">{t("inviteDescription")}</p>
          <div className="space-y-2">
            <Label htmlFor="userName">{t("userName")}</Label>
            <Input
              id="userName"
              type="text"
              autoComplete="username"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">{t("password")}</Label>
            <Input
              id="password"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">{t("confirmPassword")}</Label>
            <Input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>
          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}
        </CardContent>
        <CardFooter className="flex flex-col gap-4">
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? t("creatingAccount") : t("inviteSubmit")}
          </Button>
          <Link
            href="/login"
            className="text-center text-sm text-muted-foreground underline-offset-4 hover:underline"
          >
            {t("backToSignIn")}
          </Link>
        </CardFooter>
      </form>
    </Card>
  );
}

export default function AcceptInvitePage() {
  return (
    <Suspense
      fallback={
        <Card className="w-full max-w-sm">
          <CardContent className="pt-6">
            <p className="text-muted-foreground">Loading…</p>
          </CardContent>
        </Card>
      }
    >
      <AcceptInviteForm />
    </Suspense>
  );
}
