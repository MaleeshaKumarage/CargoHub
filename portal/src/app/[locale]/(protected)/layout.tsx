"use client";

import { useEffect } from "react";
import { useRouter } from "@/i18n/navigation";
import { useAuth } from "@/context/AuthContext";
import { Navbar } from "@/components/Navbar";
import { useTranslations } from "next-intl";

export default function ProtectedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuth();
  const t = useTranslations("common");

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <p className="text-muted-foreground">{t("loading")}</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div data-slot="layout" className="flex min-h-screen flex-col bg-background">
      <Navbar />
      <main
        data-slot="main"
        className="w-full flex-1 px-4 py-6 sm:px-6 lg:px-8"
      >
        {children}
      </main>
    </div>
  );
}
