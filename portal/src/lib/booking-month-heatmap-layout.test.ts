import { describe, it, expect } from "vitest";
import {
  buildBookingMonthGrid,
  getUtcIsoWeek,
  getUtcIsoWeekYear,
} from "./booking-month-heatmap-layout";

describe("getUtcIsoWeek / getUtcIsoWeekYear", () => {
  it("2026-01-01 (Thursday) is ISO week 1 of 2026", () => {
    expect(getUtcIsoWeekYear(2026, 0, 1)).toBe(2026);
    expect(getUtcIsoWeek(2026, 0, 1)).toBe(1);
  });

  it("2026-01-05 (Monday) is ISO week 2 of 2026", () => {
    expect(getUtcIsoWeek(2026, 0, 5)).toBe(2);
    expect(getUtcIsoWeekYear(2026, 0, 5)).toBe(2026);
  });

  it("December 2025 uses ISO weeks in 2025 (not 2026)", () => {
    expect(getUtcIsoWeekYear(2025, 11, 1)).toBe(2025);
    expect(getUtcIsoWeek(2025, 11, 1)).toBeGreaterThanOrEqual(48);
    expect(getUtcIsoWeek(2025, 11, 1)).toBeLessThanOrEqual(53);
  });
});

describe("buildBookingMonthGrid", () => {
  it("returns empty weeks for invalid month", () => {
    expect(buildBookingMonthGrid([], 2025, 0).weeks).toHaveLength(0);
    expect(buildBookingMonthGrid([], 2025, 13).weeks).toHaveLength(0);
  });

  it("uses target month, not a stale daily[0], for layout", () => {
    // Data is April 2026 but caller asks for March 2025 → grid is March 2025 (counts mostly zero).
    const april2026 = Array.from({ length: 30 }, (_, i) => ({
      date: `2026-04-${String(i + 1).padStart(2, "0")}`,
      count: 5,
    }));
    const { weeks } = buildBookingMonthGrid(april2026, 2025, 3);
    const inMarch = weeks.flatMap((w) => w.cells).filter((c) => c.inTargetMonth);
    expect(inMarch).toHaveLength(31);
    expect(inMarch[0].date).toBe("2025-03-01");
    expect(inMarch[0].count).toBe(0);
  });

  it("aligns March 2025 to real UTC weekdays and shows day-of-month in cells", () => {
    const march2025 = Array.from({ length: 31 }, (_, i) => ({
      date: `2025-03-${String(i + 1).padStart(2, "0")}`,
      count: i === 0 ? 3 : 0,
    }));
    const { weeks, maxCount } = buildBookingMonthGrid(march2025, 2025, 3);
    expect(maxCount).toBe(3);
    const inMonth = weeks.flatMap((w) => w.cells).filter((c) => c.inTargetMonth);
    expect(inMonth).toHaveLength(31);
    expect(inMonth[0].date).toBe("2025-03-01");
    expect(inMonth[0].dayOfMonth).toBe(1);
    expect(weeks[0].cells[0].date).toBe("2025-02-24");
    expect(weeks[0].cells[0].inTargetMonth).toBe(false);
  });

  it("uses ISO week on row labels (Monday row)", () => {
    const march2025 = Array.from({ length: 31 }, (_, i) => ({
      date: `2025-03-${String(i + 1).padStart(2, "0")}`,
      count: 0,
    }));
    const { weeks } = buildBookingMonthGrid(march2025, 2025, 3);
    const firstMonday = weeks[0].cells[0];
    const y = Number.parseInt(firstMonday.date.slice(0, 4), 10);
    const m0 = Number.parseInt(firstMonday.date.slice(5, 7), 10) - 1;
    const d = Number.parseInt(firstMonday.date.slice(8, 10), 10);
    expect(weeks[0].isoWeek).toBe(getUtcIsoWeek(y, m0, d));
    expect(weeks[0].isoWeekYear).toBe(getUtcIsoWeekYear(y, m0, d));
  });

  it("total cells are a multiple of 7", () => {
    const march2025 = Array.from({ length: 31 }, (_, i) => ({
      date: `2025-03-${String(i + 1).padStart(2, "0")}`,
      count: 0,
    }));
    const { weeks } = buildBookingMonthGrid(march2025, 2025, 3);
    const n = weeks.reduce((s, w) => s + w.cells.length, 0);
    expect(n % 7).toBe(0);
  });
});
