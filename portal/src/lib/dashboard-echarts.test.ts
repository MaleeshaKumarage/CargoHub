import { describe, it, expect } from "vitest";
import {
  buildPeriodBarOption,
  buildCourierPieOption,
  buildCityBarOption,
  aggregateCouriersForPie,
  buildLast7DaysGroupedBarOption,
  buildCurrencyDonutOption,
  buildMonthlyEarningsLineOption,
  buildMonetaryHorizontalBarOption,
  buildSunburstOption,
  buildSankeyOption,
  buildDailyVolumeLineOption,
  buildDeliveryBoxplotOption,
  buildDayHourHeatmapOption,
  buildTodayVsAverageBulletOption,
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

  it("tooltip formatter covers array params, tuple values, decimals, and non-numeric skip", () => {
    const daily: { date: string; count: number }[] = [];
    for (let d = 1; d <= 14; d++) {
      daily.push({ date: `2025-01-${String(d).padStart(2, "0")}`, count: 0 });
    }
    const opt = buildLast7DaysGroupedBarOption(daily, theme, "Act", "Avg", "#111", "#222", dow);
    const fmt = opt!.tooltip?.formatter as (p: unknown) => string;
    const multi = fmt([
      { dataIndex: 5, seriesName: "Act", value: 7 },
      { dataIndex: 5, seriesName: "Avg", value: 2.25 },
    ]);
    expect(multi).toContain("2025-01-13");
    expect(multi).toContain("7");
    expect(multi).toContain("2.3");
    const single = fmt({ dataIndex: 1, seriesName: "Act", value: [4] });
    expect(single).toContain("2025-01-09");
    expect(single).toContain("4");
    const skip = fmt([{ dataIndex: 0, seriesName: "Act", value: "x" }]);
    expect(skip).not.toContain("Act:");
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

describe("buildCurrencyDonutOption", () => {
  it("builds pie series with EUR formatter hook", () => {
    const fmt = (n: number) => `€${n.toFixed(2)}`;
    const opt = buildCurrencyDonutOption(
      [{ key: "Plan A", count: 60 }],
      ["#c1"],
      theme,
      fmt,
    );
    const s = opt.series?.[0] as { type: string; data: { name: string; value: number }[] };
    expect(s?.type).toBe("pie");
    expect(s?.data[0]).toMatchObject({ name: "Plan A", value: 60 });
  });

  it("tooltip formatter includes amount and percent", () => {
    const opt = buildCurrencyDonutOption([{ key: "A", count: 1 }], ["#c"], theme, (n) => `€${n}`);
    const fmt = opt.tooltip?.formatter as (p: { name?: string; value?: number; percent?: number }) => string;
    expect(fmt({ name: "A", value: 2, percent: 12.3 })).toContain("€2");
    expect(fmt({ name: "A", value: 2, percent: 12.3 })).toContain("12.3%");
  });

  it("label formatter handles missing percent", () => {
    const opt = buildCurrencyDonutOption([{ key: "A", count: 1 }], ["#c"], theme, (n) => String(n));
    const s = opt.series?.[0] as {
      label: { formatter: (p: { name?: string; percent?: number }) => string };
    };
    expect(s?.label.formatter({ name: "X" })).toContain("X");
  });
});

describe("buildMonthlyEarningsLineOption", () => {
  it("returns null for empty points", () => {
    expect(buildMonthlyEarningsLineOption([], "#f00", theme, (n) => String(n))).toBeNull();
  });

  it("builds line series for EUR trend", () => {
    const opt = buildMonthlyEarningsLineOption(
      [
        { label: "2026-01", totalEur: 100 },
        { label: "2026-02", totalEur: 250.5 },
      ],
      "#0a0",
      theme,
      (n) => n.toFixed(2),
    );
    const s = opt?.series?.[0] as { type: string; data: number[] };
    expect(s?.type).toBe("line");
    expect(s?.data).toEqual([100, 250.5]);
  });
});

describe("buildMonetaryHorizontalBarOption", () => {
  it("maps amounts to horizontal bars", () => {
    const opt = buildMonetaryHorizontalBarOption(
      [{ key: "Acme", count: 99.5 }],
      "#b00",
      theme,
      (n) => `€${n}`,
      "EUR",
    );
    expect(opt.yAxis).toMatchObject({ type: "category", data: ["Acme"], inverse: true });
    const series = opt.series?.[0];
    const data = series?.data as { value: number }[];
    expect(data[0].value).toBe(99.5);
  });
});

describe("buildSunburstOption", () => {
  it("shows empty hint when root missing or has no children", () => {
    expect(buildSunburstOption(null, ["#a", "#b"], theme, "empty").title?.text).toBe("empty");
    expect(buildSunburstOption({ name: "r", value: 1 }, ["#a", "#b"], theme, "empty").title?.text).toBe("empty");
  });

  it("builds sunburst series when children exist", () => {
    const opt = buildSunburstOption(
      { name: "root", value: 10, children: [{ name: "a", value: 4 }, { name: "b", value: 6 }] },
      ["#c0", "#c1"],
      theme,
      "empty",
    );
    const s = opt.series?.[0] as { type: string; data: unknown[] };
    expect(s?.type).toBe("sunburst");
    expect(s?.data).toHaveLength(1);
  });
});

describe("buildSankeyOption", () => {
  it("shows empty hint when there are no links", () => {
    const opt = buildSankeyOption({ nodes: [{ name: "A" }], links: [] }, theme, "no flow");
    expect(opt.title?.text).toBe("no flow");
  });

  it("maps nodes and links when graph has flow", () => {
    const opt = buildSankeyOption(
      {
        nodes: [{ name: "A" }, { name: "B" }],
        links: [{ source: "A", target: "B", value: 3 }],
      },
      theme,
      "no flow",
    );
    const s = opt.series?.[0] as { type: string; data: { name: string }[]; links: { source: string; value: number }[] };
    expect(s?.type).toBe("sankey");
    expect(s?.data).toHaveLength(2);
    expect(s?.links[0].source).toBe("A");
    expect(s?.links[0].value).toBe(3);
  });
});

describe("buildDailyVolumeLineOption", () => {
  it("uses sliced dates and line series", () => {
    const opt = buildDailyVolumeLineOption(
      [{ date: "2026-04-06", count: 2 }, { date: "2026-04-07", count: 5 }],
      "#f00",
      theme,
      "bookings",
    );
    const x = opt.xAxis as { data: string[] };
    expect(x.data).toEqual(["04-06", "04-07"]);
    const series = opt.series?.[0] as { type: string; data: number[] };
    expect(series?.type).toBe("line");
    expect(series?.data).toEqual([2, 5]);
    const fmt = opt.tooltip?.valueFormatter as (v: number) => string;
    expect(fmt(3)).toBe("3 bookings");
  });
});

describe("buildDeliveryBoxplotOption", () => {
  it("shows title when sample is empty", () => {
    const opt = buildDeliveryBoxplotOption(
      {
        sampleSize: 0,
        minHours: 0,
        q1Hours: 0,
        medianHours: 0,
        q3Hours: 0,
        maxHours: 0,
        outlierCount: 0,
        sampleHours: [],
      },
      theme,
      "Hours",
    );
    expect(opt.title?.text).toBe("Hours");
  });

  it("builds boxplot row when sample present", () => {
    const opt = buildDeliveryBoxplotOption(
      {
        sampleSize: 4,
        minHours: 1,
        q1Hours: 2,
        medianHours: 3,
        q3Hours: 4,
        maxHours: 9,
        outlierCount: 0,
        sampleHours: [],
      },
      theme,
      "h",
    );
    const s = opt.series?.[0] as { type: string; data: number[][] };
    expect(s?.type).toBe("boxplot");
    expect(s?.data[0]).toEqual([1, 2, 3, 4, 9]);
  });
});

describe("buildDayHourHeatmapOption", () => {
  it("maps cells and uses max at least 1 for visualMap", () => {
    const opt = buildDayHourHeatmapOption(
      [{ dayOfWeek: 1, hour: 3, count: 0 }],
      0,
      theme,
      ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
    );
    expect((opt.visualMap as { max: number }).max).toBe(1);
    const s = opt.series?.[0] as { type: string; data: [number, number, number][] };
    expect(s?.type).toBe("heatmap");
    expect(s?.data[0]).toEqual([3, 1, 0]);
  });
});

describe("buildTodayVsAverageBulletOption", () => {
  it("sets y max from larger of today, average, and floor", () => {
    const opt = buildTodayVsAverageBulletOption(4, 10, theme, "Today", "Avg");
    const y = opt.yAxis as { max: number };
    expect(y.max).toBe(12);
    const s = opt.series?.[0] as { type: string; data: number[]; markLine: { data: { yAxis: number }[] } };
    expect(s?.data[0]).toBe(4);
    expect(s?.markLine.data[0].yAxis).toBe(10);
  });
});
