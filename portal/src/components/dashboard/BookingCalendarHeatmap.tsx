"use client";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  buildBookingMonthGrid,
  type DailyBookingCell,
} from "@/lib/booking-month-heatmap-layout";
import { useLocale, useTranslations } from "next-intl";
import { Dialog as DialogPrimitive } from "radix-ui";
import { useState } from "react";

function formatUtcYmdLong(ymd: string, locale: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(ymd);
  if (!m) return ymd;
  const d = new Date(Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3]), 12, 0, 0));
  return new Intl.DateTimeFormat(locale, {
    weekday: "long",
    month: "long",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  }).format(d);
}

function cellSurfaceClass(
  count: number,
  max: number,
  inTargetMonth: boolean,
): string {
  if (!inTargetMonth) {
    return cn(
      "border border-border/15 bg-muted/25",
      "dark:border-white/[0.06] dark:bg-[#0f1219]",
    );
  }
  if (count <= 0) {
    return cn(
      "border border-border/25 bg-muted/40",
      "dark:border-white/5 dark:bg-[#141822]",
    );
  }
  const t = max > 0 ? count / max : 0;
  if (t <= 0.25) {
    return cn(
      "border border-blue-500/15 bg-blue-950/70",
      "dark:border-blue-500/20 dark:bg-[#1a2744]",
    );
  }
  if (t <= 0.5) {
    return "border border-blue-400/25 bg-blue-800/90 dark:bg-[#1d4ed8]/90";
  }
  if (t <= 0.75) {
    return "border border-blue-300/35 bg-blue-600 dark:bg-[#2563eb]";
  }
  return cn(
    "border border-sky-300/50 bg-blue-500",
    "shadow-[0_0_14px_rgba(59,130,246,0.55)] dark:bg-[#3b82f6] dark:shadow-[0_0_18px_rgba(96,165,250,0.45)]",
  );
}

export function BookingCalendarHeatmapGrid({
  daily,
  completedDaily,
  draftsDaily,
  targetYear,
  targetMonth,
  dowLabels,
  weekLabel,
  formatCellTooltip,
}: {
  daily: DailyBookingCell[];
  /** Non-draft counts per day (heatmap month); used for tooltips. */
  completedDaily: DailyBookingCell[];
  /** Draft counts per day (heatmap month); used for tooltips. */
  draftsDaily: DailyBookingCell[];
  /** Calendar year of the month shown (must match month selector and API request). */
  targetYear: number;
  /** 1–12, must match month selector and API request. */
  targetMonth: number;
  dowLabels: string[];
  weekLabel: (isoWeek: number, isoWeekYear: number) => string;
  formatCellTooltip: (p: { date: string; bookingCount: number; draftCount: number }) => string;
}) {
  const tStats = useTranslations("dashboard.stats");
  const locale = useLocale();
  const [dayDetail, setDayDetail] = useState<{
    date: string;
    bookingCount: number;
    draftCount: number;
  } | null>(null);

  const { weeks, maxCount } = buildBookingMonthGrid(daily, targetYear, targetMonth);
  const completedMap = new Map(completedDaily.map((x) => [x.date, x.count]));
  const draftsMap = new Map(draftsDaily.map((x) => [x.date, x.count]));

  return (
    <div className="space-y-2">
    <div
      className="grid gap-1"
      style={{
        gridTemplateColumns: `5.25rem repeat(7, minmax(0, 1fr))`,
      }}
    >
      <div />
      {dowLabels.map((d) => (
        <div
          key={d}
          className="pb-1 text-center text-[10px] font-semibold uppercase tracking-wide text-muted-foreground"
        >
          {d}
        </div>
      ))}

      {weeks.map((w) => (
        <div key={`${w.isoWeekYear}-W${w.isoWeek}-${w.cells[0]?.date}`} className="contents">
          <div className="flex items-center text-[11px] font-semibold tabular-nums text-muted-foreground">
            {weekLabel(w.isoWeek, w.isoWeekYear)}
          </div>
          {w.cells.map((c) => {
            const bookingCount = completedMap.get(c.date) ?? 0;
            const draftCount = draftsMap.get(c.date) ?? 0;
            const title = c.inTargetMonth
              ? formatCellTooltip({ date: c.date, bookingCount, draftCount })
              : c.date;
            const showCounts = c.inTargetMonth && (bookingCount > 0 || draftCount > 0);
            return (
              <div
                key={c.date}
                title={title}
                aria-label={c.inTargetMonth ? title : c.date}
                className={cn(
                  "flex aspect-square max-h-[3.25rem] min-h-[2.75rem] w-full flex-col items-center justify-center gap-0.5 rounded-md px-0.5 py-0.5 transition-colors sm:max-h-14 sm:min-h-[3rem]",
                  cellSurfaceClass(c.count, maxCount, c.inTargetMonth),
                )}
              >
                <span
                  className={cn(
                    "text-sm font-semibold tabular-nums leading-none",
                    c.inTargetMonth ? "text-foreground" : "text-muted-foreground/45",
                  )}
                >
                  {c.dayOfMonth}
                </span>
                {showCounts ? (
                  <span
                    className="max-w-full truncate text-center text-[9px] font-semibold tabular-nums leading-none text-foreground/95 sm:text-[10px]"
                    aria-hidden
                  >
                    {tStats("heatmapCellInlineCounts", { bookingCount, draftCount })}
                  </span>
                ) : null}
              </div>
            );
          })}
        </div>
      ))}
    </div>
    <p className="text-center text-[11px] leading-snug text-muted-foreground sm:text-left">
      {tStats("heatmapLegendTapDay")}
    </p>

    <DialogPrimitive.Root
      open={dayDetail !== null}
      onOpenChange={(open) => {
        if (!open) setDayDetail(null);
      }}
    >
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50" />
        <DialogPrimitive.Content
          className={cn(
            "data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95",
            "fixed left-[50%] top-[50%] z-50 w-[min(20rem,calc(100vw-2rem))] translate-x-[-50%] translate-y-[-50%] rounded-xl border bg-background p-4 shadow-lg",
          )}
        >
          {dayDetail ? (
            <>
              <DialogPrimitive.Title className="text-base font-semibold leading-snug tracking-tight">
                {formatUtcYmdLong(dayDetail.date, locale)}
              </DialogPrimitive.Title>
              <DialogPrimitive.Description className="mt-3 space-y-2.5 text-foreground">
                <span className="block text-[15px] font-medium leading-6">
                  {tStats("heatmapDayDialogBookings", { count: dayDetail.bookingCount })}
                </span>
                <span className="block text-[15px] font-medium leading-6">
                  {tStats("heatmapDayDialogDrafts", { count: dayDetail.draftCount })}
                </span>
              </DialogPrimitive.Description>
              <div className="mt-4 flex justify-end">
                <DialogPrimitive.Close asChild>
                  <Button type="button" variant="outline" size="sm">
                    {tStats("heatmapDayDialogClose")}
                  </Button>
                </DialogPrimitive.Close>
              </div>
            </>
          ) : null}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
    </div>
  );
}
