"use client";

import { useAuth } from "@/context/AuthContext";
import { useBranding } from "@/context/BrandingContext";
import { useDesignTheme } from "@/context/DesignThemeContext";
import { getMe } from "@/lib/api";
import { getRolesFromToken } from "@/lib/jwt";
import { DESIGN_THEMES, type DesignTheme } from "@/lib/api";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import { useLocale } from "next-intl";
import { useEffect, useState } from "react";
import { Palette } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { isRiderOnlyPortal } from "@/lib/rider-role";

export function Navbar() {
  const { theme, setTheme } = useDesignTheme();
  const { user, token, logout } = useAuth();
  const branding = useBranding();
  const router = useRouter();
  const pathname = usePathname();
  const locale = useLocale();
  const t = useTranslations("common");
  const tNav = useTranslations("nav");
  const tDesignTheme = useTranslations("designTheme");
  const [rolesFromMe, setRolesFromMe] = useState<string[] | null>(null);
  const appName = branding.appName || t("appName");

  // When user has token but no roles in context (e.g. Super Admin from bootstrap), fetch /me so "Manage Users" appears
  useEffect(() => {
    const fromUser = user?.roles ?? (user as { Roles?: string[] })?.Roles;
    if (fromUser && fromUser.length > 0) return;
    if (!token) return;
    getMe(token)
      .then((me) => setRolesFromMe(me.roles))
      .catch(() => setRolesFromMe([]));
  }, [token, user?.roles, user?.userId]);

  function handleSignOut() {
    logout();
    router.replace("/login");
  }

  const displayName = user?.displayName || user?.email || "";
  const rolesFromUser = user?.roles ?? (user as { Roles?: string[] })?.Roles ?? [];
  const rolesFromToken = getRolesFromToken(token ?? undefined);
  const roles =
    rolesFromUser.length > 0
      ? rolesFromUser
      : (rolesFromMe ?? rolesFromToken ?? []);
  const isSuperAdmin = Array.isArray(roles) && roles.includes("SuperAdmin");
  const isRiderOnly = isRiderOnlyPortal(roles);

  if (isRiderOnly) {
    return (
      <header data-slot="navbar" className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 w-full items-center justify-between px-4 sm:px-6 lg:px-8">
          <Link href="/rider/deliveries" className="flex items-center gap-2 font-semibold">
            {branding.logoUrl ? (
              <img src={branding.logoUrl} alt={appName} className="h-8 w-auto object-contain" />
            ) : (
              <span className="truncate">{appName}</span>
            )}
          </Link>
          <nav className="flex items-center gap-2">
            <Link href="/rider/deliveries">
              <Button variant={pathname?.startsWith("/rider/deliveries") ? "secondary" : "ghost"} size="sm">
                {tNav("riderDeliveries")}
              </Button>
            </Link>
            <Link href="/rider/profile">
              <Button variant={pathname?.startsWith("/rider/profile") ? "secondary" : "ghost"} size="sm">
                {tNav("riderProfile")}
              </Button>
            </Link>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" title={tDesignTheme("label")}>
                  <Palette className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {DESIGN_THEMES.map((tKey) => (
                  <DropdownMenuItem
                    key={tKey}
                    onClick={() => setTheme(tKey as DesignTheme)}
                    className={theme === tKey ? "bg-accent" : undefined}
                  >
                    {tDesignTheme(tKey)}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm">
                  {locale.toUpperCase()}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem asChild>
                  <Link href={pathname ?? "/rider/deliveries"} locale="en">
                    English
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href={pathname ?? "/rider/deliveries"} locale="fi">
                    Suomi
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href={pathname ?? "/rider/deliveries"} locale="sv">
                    Svenska
                  </Link>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm">
                  {displayName || t("account")}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>{displayName}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleSignOut}>{t("signOut")}</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </nav>
        </div>
      </header>
    );
  }

  return (
    <header data-slot="navbar" className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="flex h-14 w-full items-center justify-between px-4 sm:px-6 lg:px-8">
        <Link href="/dashboard" className="flex items-center gap-2 font-semibold">
          {branding.logoUrl ? (
            <img
              src={branding.logoUrl}
              alt={appName}
              className="h-8 w-auto object-contain"
            />
          ) : (
            <span className="truncate">{appName}</span>
          )}
        </Link>
        <nav className="flex items-center gap-2">
          <Link href="/dashboard">
            <Button variant={pathname === "/dashboard" ? "secondary" : "ghost"} size="sm">
              {tNav("dashboard")}
            </Button>
          </Link>
          <Link href="/bookings">
            <Button variant={pathname?.startsWith("/bookings") ? "secondary" : "ghost"} size="sm">
              {tNav("bookings")}
            </Button>
          </Link>
          <Link href="/actions">
            <Button variant={pathname === "/actions" ? "secondary" : "ghost"} size="sm">
              {tNav("actions")}
            </Button>
          </Link>
          <Link href="/plugin">
            <Button variant={pathname === "/plugin" ? "secondary" : "ghost"} size="sm">
              {tNav("plugin")}
            </Button>
          </Link>
          <Link href="/more">
            <Button variant={pathname === "/more" ? "secondary" : "ghost"} size="sm">
              {tNav("more")}
            </Button>
          </Link>
          {isSuperAdmin && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant={pathname?.startsWith("/manage") ? "secondary" : "ghost"}
                  size="sm"
                >
                  {tNav("manage")}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                <DropdownMenuItem asChild>
                  <Link href="/manage/users">{tNav("users")}</Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/manage/companies">{tNav("companies")}</Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/manage/freelance-riders">{tNav("freelanceRiders")}</Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/manage/invoices">{tNav("invoices")}</Link>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" title={tDesignTheme("label")}>
                <Palette className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {DESIGN_THEMES.map((tKey) => (
                <DropdownMenuItem
                  key={tKey}
                  onClick={() => setTheme(tKey as DesignTheme)}
                  className={theme === tKey ? "bg-accent" : undefined}
                >
                  {tDesignTheme(tKey)}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm">
                {locale.toUpperCase()}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="en">
                  English
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="fi">
                  Suomi
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="sv">
                  Svenska
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="no">
                  Norsk
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="da">
                  Dansk
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/dashboard"} locale="is">
                  Íslenska
                </Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm">
                {displayName || t("account")}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>{displayName}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleSignOut}>
                {t("signOut")}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </nav>
      </div>
    </header>
  );
}
