"use client";

import { useAuth } from "@/context/AuthContext";
import { getMe } from "@/lib/api";
import { getRolesFromToken } from "@/lib/jwt";
import { useRouter, usePathname, Link } from "@/i18n/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";

export default function ManageLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { user, token, updateUserRoles } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [accessAllowed, setAccessAllowed] = useState<boolean | null>(null);

  const rolesFromUser = user?.roles ?? (user as { Roles?: string[] })?.Roles ?? [];
  const rolesFromToken = getRolesFromToken(token ?? undefined);
  const roles = rolesFromUser.length > 0 ? rolesFromUser : rolesFromToken;
  const isSuperAdminFromContext = Array.isArray(roles) && roles.includes("SuperAdmin");

  useEffect(() => {
    if (isSuperAdminFromContext && accessAllowed === null) setAccessAllowed(true);
  }, [isSuperAdminFromContext, accessAllowed]);

  useEffect(() => {
    if (!token) {
      router.replace("/dashboard");
      return;
    }
    if (isSuperAdminFromContext) return;
    if (accessAllowed === true) return;
    let cancelled = false;
    getMe(token)
      .then((me) => {
        if (cancelled) return;
        if (me.roles?.includes("SuperAdmin")) {
          updateUserRoles(me.roles);
          setAccessAllowed(true);
        } else {
          router.replace("/dashboard");
        }
      })
      .catch(() => {
        if (!cancelled) router.replace("/dashboard");
      });
    return () => {
      cancelled = true;
    };
  }, [token, isSuperAdminFromContext, accessAllowed, router, updateUserRoles]);

  const isSuperAdmin = isSuperAdminFromContext || accessAllowed === true;

  if (!token) return null;
  if (token && !isSuperAdmin) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-muted-foreground">Loading…</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <nav className="flex items-center gap-2 border-b pb-4">
        <Link href="/manage/users">
          <Button
            variant={pathname?.startsWith("/manage/users") ? "secondary" : "ghost"}
            size="sm"
          >
            Users
          </Button>
        </Link>
        <Link href="/manage/companies">
          <Button
            variant={pathname?.startsWith("/manage/companies") ? "secondary" : "ghost"}
            size="sm"
          >
            Companies
          </Button>
        </Link>
      </nav>
      {children}
    </div>
  );
}
