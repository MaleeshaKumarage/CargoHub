"use client";

import { useBranding } from "@/context/BrandingContext";
import { Link, usePathname } from "@/i18n/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
const navKeys = [
  "top",
  "platform",
  "bookings",
  "dashboard",
  "workflow",
  "carriers",
  "admin",
  "start",
] as const;

export function MarketingTourHeader() {
  const branding = useBranding();
  const t = useTranslations("tour.nav");
  const tCta = useTranslations("tour.cta");
  const tLogo = useTranslations("tour");
  const tCommon = useTranslations("common");
  const pathname = usePathname();
  const locale = useLocale();
  const appName = branding.appName || tCommon("appName");

  return (
    <header className="sticky top-0 z-50 border-b border-cyan-500/20 bg-zinc-950/90 backdrop-blur-md">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
        <a href="#top" className="flex items-center gap-2 shrink-0">
          <img
            src="/branding/cargohub-logo-on-dark.svg"
            alt={tLogo("logoAlt")}
            className="h-8 w-auto object-contain"
          />
          <span className="hidden font-semibold text-zinc-100 sm:inline">{appName}</span>
        </a>
        <nav className="hidden items-center gap-1 lg:flex" aria-label="Tour sections">
          {navKeys.map((key) => (
            <a
              key={key}
              href={`#${key}`}
              className="rounded-md px-2 py-1.5 font-mono text-[11px] uppercase tracking-wider text-zinc-400 transition-colors hover:bg-cyan-500/10 hover:text-cyan-300"
            >
              {t(key)}
            </a>
          ))}
        </nav>
        <div className="flex items-center gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="font-mono text-xs text-zinc-300 hover:bg-cyan-500/10 hover:text-cyan-200"
              >
                {locale.toUpperCase()}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="min-w-[8rem]">
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="en">
                  English
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="fi">
                  Suomi
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="sv">
                  Svenska
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="no">
                  Norsk
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="da">
                  Dansk
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={pathname ?? "/tour"} locale="is">
                  Íslenska
                </Link>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <Button
            variant="ghost"
            size="sm"
            asChild
            className="hidden font-mono text-xs text-zinc-300 sm:inline-flex hover:bg-cyan-500/10 hover:text-cyan-200"
          >
            <Link href="/login">{tCta("signIn")}</Link>
          </Button>
          <Button
            size="sm"
            asChild
            className="border border-cyan-500/50 bg-cyan-500/15 font-mono text-xs text-cyan-100 hover:bg-cyan-500/25"
          >
            <Link href="/register">{tCta("register")}</Link>
          </Button>
        </div>
      </div>
    </header>
  );
}
