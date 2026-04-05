"use client";

import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";

type Props = {
  open: boolean;
  onDismiss: () => void;
  /** API message (e.g. "Trial booking allowance has been used.") */
  detailMessage: string;
};

export function TrialBookingLimitBanner({ open, onDismiss, detailMessage }: Props) {
  const t = useTranslations("bookings");
  if (!open) return null;
  return (
    <div className="fixed inset-x-0 top-0 z-[100] flex justify-center px-3 pt-3 sm:px-4 sm:pt-4 pointer-events-none">
      <div
        className="pointer-events-auto flex w-full max-w-2xl items-start gap-3 rounded-lg border border-amber-500/60 bg-amber-50 text-amber-950 shadow-lg dark:border-amber-400/50 dark:bg-amber-950/95 dark:text-amber-50 p-4"
        role="alert"
        aria-live="assertive"
      >
        <div className="flex-1 min-w-0 space-y-2">
          <p className="font-semibold text-base leading-snug">{t("trialLimitBannerTitle")}</p>
          <p className="text-sm font-medium leading-snug">{detailMessage}</p>
          <p className="text-sm opacity-90 leading-relaxed">{t("trialLimitBannerHint")}</p>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-8 w-8 shrink-0 p-0 text-lg leading-none text-current hover:bg-amber-500/20 dark:hover:bg-amber-400/15"
          onClick={onDismiss}
          aria-label={t("trialLimitBannerDismiss")}
        >
          ×
        </Button>
      </div>
    </div>
  );
}
