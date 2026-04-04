export type DailyBookingCell = { date: string; count: number };

export type CalendarHeatCell = {
  /** yyyy-MM-dd (UTC calendar day). */
  date: string;
  dayOfMonth: number;
  inTargetMonth: boolean;
  count: number;
};

export type CalendarHeatWeek = {
  /** ISO 8601 week number (1–53). */
  isoWeek: number;
  /** ISO week-year (may differ from calendar year at year boundaries). */
  isoWeekYear: number;
  cells: CalendarHeatCell[];
};

const DAY_MS = 86400000;

/** ISO week-year for the UTC date (year of the Thursday of this calendar week). */
export function getUtcIsoWeekYear(utcY: number, utcM0: number, utcD: number): number {
  const noon = Date.UTC(utcY, utcM0, utcD, 12, 0, 0);
  const dow = (new Date(noon).getUTCDay() + 6) % 7;
  const thursdayMs = noon + (3 - dow) * DAY_MS;
  return new Date(thursdayMs).getUTCFullYear();
}

/** ISO 8601 week number for the UTC calendar day (Monday–Sunday weeks). */
export function getUtcIsoWeek(utcY: number, utcM0: number, utcD: number): number {
  const isoYear = getUtcIsoWeekYear(utcY, utcM0, utcD);
  const noon = Date.UTC(utcY, utcM0, utcD, 12, 0, 0);
  const dow = (new Date(noon).getUTCDay() + 6) % 7;
  const mondayMs = noon - dow * DAY_MS;
  const jan4 = Date.UTC(isoYear, 0, 4, 12, 0, 0);
  const jan4Dow = (new Date(jan4).getUTCDay() + 6) % 7;
  const week1Monday = jan4 - jan4Dow * DAY_MS;
  return Math.floor((mondayMs - week1Monday) / (7 * DAY_MS)) + 1;
}

function parseYmd(s: string): { y: number; m0: number; d: number } {
  const y = Number.parseInt(s.slice(0, 4), 10);
  const m0 = Number.parseInt(s.slice(5, 7), 10) - 1;
  const d = Number.parseInt(s.slice(8, 10), 10);
  return { y, m0, d };
}

function daysInUtcMonth(year: number, month1Based: number): number {
  const m0 = month1Based - 1;
  return new Date(Date.UTC(year, m0 + 1, 0)).getUTCDate();
}

/**
 * Full Monday–Sunday rows for one calendar month (UTC), including adjacent days so
 * weeks align. Counts only apply to days inside the target month.
 *
 * `targetYear` / `targetMonth1Based` must match the month the API was asked for; do not
 * infer only from `daily[0]` or the grid can disagree with the month selector after navigation.
 */
export function buildBookingMonthGrid(
  daily: DailyBookingCell[],
  targetYear: number,
  targetMonth1Based: number,
): {
  weeks: CalendarHeatWeek[];
  maxCount: number;
} {
  if (targetMonth1Based < 1 || targetMonth1Based > 12) {
    return { weeks: [], maxCount: 0 };
  }

  const y = targetYear;
  const m0 = targetMonth1Based - 1;
  const dim = daysInUtcMonth(targetYear, targetMonth1Based);
  const countMap = new Map(daily.map((x) => [x.date, x.count]));

  const monthStart = new Date(Date.UTC(y, m0, 1, 12, 0, 0));
  const dowStart = (monthStart.getUTCDay() + 6) % 7;
  const firstMondayMs = monthStart.getTime() - dowStart * DAY_MS;

  const monthEnd = new Date(Date.UTC(y, m0, dim, 12, 0, 0));
  const dowEnd = (monthEnd.getUTCDay() + 6) % 7;
  const lastSundayMs = monthEnd.getTime() + (6 - dowEnd) * DAY_MS;

  const flat: CalendarHeatCell[] = [];
  for (let t = firstMondayMs; t <= lastSundayMs; t += DAY_MS) {
    const cd = new Date(t);
    const yy = cd.getUTCFullYear();
    const mm = cd.getUTCMonth();
    const dd = cd.getUTCDate();
    const dateStr = `${yy}-${String(mm + 1).padStart(2, "0")}-${String(dd).padStart(2, "0")}`;
    const inTargetMonth = yy === y && mm === m0;
    const count = inTargetMonth ? (countMap.get(dateStr) ?? 0) : 0;
    flat.push({
      date: dateStr,
      dayOfMonth: dd,
      inTargetMonth,
      count,
    });
  }

  const inMonthCounts = flat.filter((c) => c.inTargetMonth).map((c) => c.count);
  const maxCount = Math.max(1, ...inMonthCounts);

  const weeks: CalendarHeatWeek[] = [];
  for (let i = 0; i < flat.length; i += 7) {
    const cells = flat.slice(i, i + 7);
    const mon = cells[0];
    const { y: my, m0: mm0, d: md } = parseYmd(mon.date);
    weeks.push({
      isoWeek: getUtcIsoWeek(my, mm0, md),
      isoWeekYear: getUtcIsoWeekYear(my, mm0, md),
      cells,
    });
  }

  return { weeks, maxCount };
}
