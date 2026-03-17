"use client";

import { useTranslations } from "next-intl";

/** Backend status values; order defines milestone progression. */
const STATUS_ORDER = ["Draft", "CompletedBooking", "Waybill", "SendBooking", "Confirmed", "Delivered"] as const;

const STAGES = [
  { key: "draft", translationKey: "draft", status: "Draft" },
  { key: "completed", translationKey: "completedBooking", status: "CompletedBooking" },
  { key: "waybill", translationKey: "waybill", status: "Waybill" },
  { key: "send", translationKey: "sendBooking", status: "SendBooking" },
  { key: "confirmed", translationKey: "confirmed", status: "Confirmed" },
  { key: "delivered", translationKey: "delivered", status: "Delivered" },
] as const;

type MilestoneItem = {
  isDraft?: boolean;
  waybillNumber?: string | null;
  statusHistory?: { status: string; occurredAtUtc: string; source?: string | null }[];
};

/** Returns 1–6: current milestone index (1-based). Uses statusHistory when present, else isDraft/waybillNumber. */
function getCurrentStage(item: MilestoneItem): number {
  const history = item.statusHistory;
  if (history?.length) {
    const completedStatuses = new Set(history.map((e) => e.status));
    for (let i = 1; i <= STATUS_ORDER.length; i++) {
      if (!completedStatuses.has(STATUS_ORDER[i - 1])) return i;
    }
    return 6;
  }
  if (item.isDraft) return 1;
  if (!item.waybillNumber || !item.waybillNumber.trim()) return 2;
  return 3;
}

/** Returns set of 1-based step numbers that are completed. When using statusHistory, fills continuously up to the highest reached milestone so the bar never has a gap. */
function getCompletedSteps(item: MilestoneItem): Set<number> {
  const history = item.statusHistory;
  if (history?.length) {
    const statusSet = new Set(history.map((e) => e.status));
    // Find highest completed step index (1-based); then all steps 1..highest are filled (continuous bar).
    let highest = 0;
    for (let i = 0; i < STATUS_ORDER.length; i++) {
      if (statusSet.has(STATUS_ORDER[i])) highest = i + 1;
    }
    const completed = new Set<number>();
    for (let step = 1; step <= highest; step++) completed.add(step);
    return completed;
  }
  const current = getCurrentStage(item);
  const out = new Set<number>();
  for (let i = 1; i < current; i++) out.add(i);
  if (!item.isDraft && item.waybillNumber?.trim()) out.add(3);
  return out;
}

/** Returns the index of the current (highest reached) milestone for translation lookup. */
function getCurrentStatusIndex(item: MilestoneItem): number {
  const history = item.statusHistory;
  if (history?.length) {
    const statusSet = new Set(history.map((e) => e.status));
    let highest = 0;
    for (let i = 0; i < STATUS_ORDER.length; i++) {
      if (statusSet.has(STATUS_ORDER[i])) highest = i + 1;
    }
    return Math.max(0, highest - 1);
  }
  if (item.isDraft) return 0;
  if (!item.waybillNumber || !item.waybillNumber.trim()) return 1;
  return 2;
}

type BookingMilestoneBarProps = {
  item: MilestoneItem;
  className?: string;
};

/** Progress bar + current status name under it. Each segment shows milestone name and date on hover when statusHistory is used. */
export function BookingMilestoneBar({ item, className = "" }: BookingMilestoneBarProps) {
  const t = useTranslations("milestones");
  const current = getCurrentStage(item);
  const completedSteps = getCompletedSteps(item);
  const currentStatusIndex = getCurrentStatusIndex(item);
  const currentLabel = t(STAGES[currentStatusIndex].translationKey);
  const historyByStatus = (item.statusHistory ?? []).reduce(
    (acc, e) => {
      acc[e.status] = e.occurredAtUtc;
      return acc;
    },
    {} as Record<string, string>
  );

  return (
    <div
      data-slot="milestone-bar"
      className={`flex flex-col gap-1 ${className}`}
      role="progressbar"
      aria-valuenow={current}
      aria-valuemin={1}
      aria-valuemax={6}
      aria-label={`Booking milestone: ${currentLabel}`}
    >
      <div className="flex items-center gap-0.5 rounded-full overflow-hidden bg-muted h-2">
        {STAGES.map((stage, i) => {
          const step = i + 1;
          const done = completedSteps.has(step);
          const date = historyByStatus[stage.status];
          const label = t(stage.translationKey);
          const title = date ? `${label} – ${new Date(date).toLocaleString()}` : label;
          return (
            <div
              key={stage.key}
              title={title}
              className={`flex-1 min-w-0 h-full transition-colors cursor-default ${
                done ? "bg-primary" : "bg-muted-foreground/20"
              }`}
            />
          );
        })}
      </div>
      <span className="text-xs text-muted-foreground font-medium">{currentLabel}</span>
    </div>
  );
}
