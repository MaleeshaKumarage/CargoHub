"use client";

import { cn } from "@/lib/utils";
import {
  buildBookingMonthGrid,
  type DailyBookingCell,
} from "@/lib/booking-month-heatmap-layout";

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
  const { weeks, maxCount } = buildBookingMonthGrid(daily, targetYear, targetMonth);
  const completedMap = new Map(completedDaily.map((x) => [x.date, x.count]));
  const draftsMap = new Map(draftsDaily.map((x) => [x.date, x.count]));

  return (
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
            return (
              <div
                key={c.date}
                title={title}
                className={cn(
                  "flex aspect-square max-h-12 min-h-[2.5rem] w-full flex-col items-center justify-center rounded-md px-0.5 transition-colors",
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
              </div>
            );
          })}
        </div>
      ))}
    </div>
  );
}
