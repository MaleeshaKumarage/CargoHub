"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";

export default function LocaleRootPage() {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (isLoading) return;
    if (isAuthenticated) {
      router.replace("/dashboard");
    } else {
      router.replace("/login");
    }
  }, [isAuthenticated, isLoading, router]);

  const t = useTranslations("common");
  return (
    <div
      className="flex min-h-screen items-center justify-center bg-background"
      suppressHydrationWarning
    >
      <p className="text-muted-foreground">{t("redirecting")}</p>
    </div>
  );
}
