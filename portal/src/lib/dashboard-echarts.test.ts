import { describe, it, expect } from "vitest";
import {
  buildPeriodBarOption,
  buildCourierPieOption,
  buildCityBarOption,
  aggregateCouriersForPie,
  buildLast7DaysGroupedBarOption,
} from "./dashboard-echarts";

const theme = {
  border: "#ccc",
  foreground: "#111",
  mutedForeground: "#666",
  card: "#fff",
};

describe("buildPeriodBarOption", () => {
  it("maps categories and per-bar colors", () => {
    const opt = buildPeriodBarOption(
      ["A", "B"],
      [3, 7],
      ["#a00", "#0a0"],
      theme,
      "bookings",
    );
    const series = opt.series?.[0] as { type: string; emphasis?: { focus?: string } };
    expect(series?.type).toBe("bar");
    expect(series?.emphasis?.focus).toBe("none");
    const data = series?.data as { value: number; itemStyle: { color: string } }[];
    expect(data[0].value).toBe(3);
    expect(data[0].itemStyle.color).toBe("#a00");
    expect(data[1].value).toBe(7);
    expect(opt.xAxis).toMatchObject({ type: "category", data: ["A", "B"] });
    expect(opt.tooltip?.axisPointer).toMatchObject({ type: "line" });
  });
});

describe("buildCourierPieOption", () => {
  it("builds pie data with colors cycling", () => {
    const opt = buildCourierPieOption(
      [
        { key: "DHL", count: 10 },
        { key: "Posti", count: 5 },
      ],
      ["#c1", "#c2"],
      theme,
      "bookings",
    );
    const s = opt.series?.[0] as { type: string; radius: string[]; data: unknown[] };
    expect(s?.type).toBe("pie");
    expect(s?.radius).toEqual(["44%", "72%"]);
    const data = s?.data as { name: string; value: number; itemStyle: { color: string } }[];
    expect(data[0]).toMatchObject({ name: "DHL", value: 10, itemStyle: { color: "#c1" } });
    expect(data[1]).toMatchObject({ name: "Posti", value: 5, itemStyle: { color: "#c2" } });
  });
});

describe("aggregateCouriersForPie", () => {
  it("returns top N and rolls the rest into Other", () => {
    const out = aggregateCouriersForPie(
      [
        { key: "A", count: 10 },
        { key: "B", count: 8 },
        { key: "C", count: 5 },
        { key: "D", count: 2 },
      ],
      2,
      "Other",
    );
    expect(out).toEqual([
      { key: "A", count: 10 },
      { key: "B", count: 8 },
      { key: "Other", count: 7 },
    ]);
  });

  it("omits Other when nothing remains", () => {
    const out = aggregateCouriersForPie(
      [
        { key: "A", count: 1 },
        { key: "B", count: 1 },
      ],
      2,
      "Other",
    );
    expect(out).toEqual([
      { key: "A", count: 1 },
      { key: "B", count: 1 },
    ]);
  });
});

describe("buildLast7DaysGroupedBarOption", () => {
  const dow = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  it("returns null for empty daily", () => {
    expect(buildLast7DaysGroupedBarOption([], theme, "B", "Avg", "#111", "#222", dow)).toBeNull();
  });

  it("builds two bar series with per-weekday averages over the window", () => {
    const daily: { date: string; count: number }[] = [];
    for (let d = 1; d <= 14; d++) {
      const date = `2025-01-${String(d).padStart(2, "0")}`;
      const count = d === 6 ? 4 : d === 13 ? 8 : 0;
      daily.push({ date, count });
    }
    const opt = buildLast7DaysGroupedBarOption(daily, theme, "B", "Avg", "#111", "#222", dow);
    expect(opt).not.toBeNull();
    const series = opt!.series as { type: string; name: string; data: { value: number }[] }[];
    expect(series).toHaveLength(2);
    expect(series[0].type).toBe("bar");
    expect(series[1].type).toBe("bar");
    expect(series[0].data).toHaveLength(7);
    expect(series[1].data).toHaveLength(7);
    // last7 ends 2025-01-14 … 2025-01-08; index 5 is Monday 2025-01-13 → avg of Mon = (4+8)/2
    expect(series[1].data[5].value).toBe(6);
  });
});

describe("buildCityBarOption", () => {
  it("uses inverse category axis for horizontal layout", () => {
    const opt = buildCityBarOption(
      [
        { key: "Helsinki", count: 4 },
        { key: "Tampere", count: 2 },
      ],
      "#00f",
      theme,
      "bookings",
    );
    expect(opt.yAxis).toMatchObject({
      type: "category",
      data: ["Helsinki", "Tampere"],
      inverse: true,
    });
    const series = opt.series?.[0];
    const data = series?.data as { value: number; itemStyle: { color: string } }[];
    expect(data[0].value).toBe(4);
    expect(data[0].itemStyle.color).toBe("#00f");
  });
});

