import type { EChartsOption } from "echarts";

export type ThemeSlice = {
  border: string;
  foreground: string;
  mutedForeground: string;
  card: string;
};

/**
 * Period comparison: vertical bars, one color per category.
 */
export function buildPeriodBarOption(
  categories: string[],
  values: number[],
  barColors: string[],
  theme: ThemeSlice,
  bookingsLabel: string,
): EChartsOption {
  return {
    grid: { left: 8, right: 8, top: 8, bottom: 8, containLabel: true },
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: theme.card,
      borderColor: theme.border,
      borderWidth: 1,
      textStyle: { color: theme.foreground },
      valueFormatter: (v) => `${v} ${bookingsLabel}`,
    },
    xAxis: {
      type: "category",
      data: categories,
      axisLine: { lineStyle: { color: theme.border } },
      axisLabel: { color: theme.mutedForeground, fontSize: 12 },
    },
    yAxis: {
      type: "value",
      minInterval: 1,
      axisLine: { show: false },
      axisLabel: { color: theme.mutedForeground, fontSize: 12 },
      splitLine: { lineStyle: { color: theme.border, opacity: 0.45 } },
    },
    series: [
      {
        type: "bar",
        name: bookingsLabel,
        data: values.map((value, i) => ({
          value,
          itemStyle: { color: barColors[i % barColors.length], borderRadius: [4, 4, 0, 0] },
        })),
        barMaxWidth: 56,
      },
    ],
  };
}

export type KeyCount = { key: string; count: number };

/**
 * Donut: share by courier / carrier.
 */
export function buildCourierPieOption(
  rows: KeyCount[],
  seriesColors: string[],
  theme: ThemeSlice,
  bookingsLabel: string,
): EChartsOption {
  return {
    tooltip: {
      trigger: "item",
      backgroundColor: theme.card,
      borderColor: theme.border,
      borderWidth: 1,
      textStyle: { color: theme.foreground },
      // String formatter avoids strict CallbackDataParams typing; {b} name, {c} value, {d} percent.
      formatter: "{b}<br/>{c} " + bookingsLabel + " ({d}%)",
    },
    series: [
      {
        type: "pie",
        radius: ["40%", "64%"],
        padAngle: 2,
        itemStyle: { borderRadius: 4 },
        label: {
          color: theme.mutedForeground,
          formatter: "{b} {d}%",
        },
        data: rows.map((r, i) => ({
          name: r.key,
          value: r.count,
          itemStyle: { color: seriesColors[i % seriesColors.length] },
        })),
      },
    ],
  };
}

/**
 * Horizontal bars: top cities (origin or destination).
 */
export function buildCityBarOption(
  rows: KeyCount[],
  barColor: string,
  theme: ThemeSlice,
  bookingsLabel: string,
): EChartsOption {
  const keys = rows.map((r) => r.key);
  const counts = rows.map((r) => r.count);
  return {
    grid: { left: 4, right: 8, top: 4, bottom: 4, containLabel: true },
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: theme.card,
      borderColor: theme.border,
      borderWidth: 1,
      textStyle: { color: theme.foreground },
      valueFormatter: (v) => `${v} ${bookingsLabel}`,
    },
    xAxis: {
      type: "value",
      minInterval: 1,
      axisLine: { show: false },
      axisLabel: { color: theme.mutedForeground, fontSize: 11 },
      splitLine: { lineStyle: { color: theme.border, opacity: 0.45 } },
    },
    yAxis: {
      type: "category",
      data: keys,
      inverse: true,
      axisLine: { show: false },
      axisTick: { show: false },
      axisLabel: { color: theme.mutedForeground, fontSize: 11, width: 72, overflow: "truncate" },
    },
    series: [
      {
        type: "bar",
        name: bookingsLabel,
        data: counts.map((value) => ({
          value,
          itemStyle: { color: barColor, borderRadius: [0, 4, 4, 0] },
        })),
        barMaxWidth: 22,
      },
    ],
  };
}

// --- Advanced dashboard (Sankey, Sunburst, time slider, box plot, heatmap, bullet) ---

export type SunburstNode = { name: string; value: number; children?: SunburstNode[] };

export function buildSunburstOption(
  root: SunburstNode | null | undefined,
  seriesColors: string[],
  theme: ThemeSlice,
  emptyHint: string,
): EChartsOption {
  if (!root || !root.children?.length) {
    return {
      title: {
        text: emptyHint,
        left: "center",
        top: "middle",
        textStyle: { color: theme.mutedForeground, fontSize: 13 },
      },
    };
  }
  return {
    tooltip: {
      trigger: "item",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
    },
    series: [
      {
        type: "sunburst",
        data: [root],
        radius: [0, "92%"],
        itemStyle: { borderRadius: 4, borderWidth: 1, borderColor: theme.border },
        label: { color: theme.mutedForeground, fontSize: 11 },
        levels: [
          { r0: "0%", r: "38%", itemStyle: { color: seriesColors[0] } },
          { r0: "38%", r: "90%", itemStyle: { color: seriesColors[1] }, label: { rotate: "tangential" } },
        ],
      },
    ],
  };
}

export type SankeyGraph = {
  nodes: { name: string }[];
  links: { source: string; target: string; value: number }[];
};

export function buildSankeyOption(graph: SankeyGraph, theme: ThemeSlice, emptyHint: string): EChartsOption {
  if (!graph.links.length) {
    return {
      title: {
        text: emptyHint,
        left: "center",
        top: "middle",
        textStyle: { color: theme.mutedForeground, fontSize: 13 },
      },
    };
  }
  return {
    tooltip: {
      trigger: "item",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
    },
    series: [
      {
        type: "sankey",
        emphasis: { focus: "adjacency" },
        data: graph.nodes.map((n) => ({ name: n.name })),
        links: graph.links.map((l) => ({
          source: l.source,
          target: l.target,
          value: l.value,
        })),
        lineStyle: { color: "gradient", curveness: 0.5, opacity: 0.45 },
        label: { color: theme.mutedForeground, fontSize: 10 },
      },
    ],
  };
}

export function buildDailyVolumeLineOption(
  daily: { date: string; count: number }[],
  lineColor: string,
  theme: ThemeSlice,
  bookingsLabel: string,
): EChartsOption {
  const cats = daily.map((d) => d.date.slice(5));
  const vals = daily.map((d) => d.count);
  return {
    grid: { left: 48, right: 16, top: 24, bottom: 56, containLabel: true },
    tooltip: {
      trigger: "axis",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
      valueFormatter: (v) => `${v} ${bookingsLabel}`,
    },
    dataZoom: [
      { type: "inside", xAxisIndex: 0, filterMode: "none" },
      { type: "slider", xAxisIndex: 0, height: 22, bottom: 8, filterMode: "none" },
    ],
    xAxis: {
      type: "category",
      data: cats,
      boundaryGap: false,
      axisLabel: { color: theme.mutedForeground, fontSize: 10, rotate: 35 },
      axisLine: { lineStyle: { color: theme.border } },
    },
    yAxis: {
      type: "value",
      minInterval: 1,
      axisLabel: { color: theme.mutedForeground },
      splitLine: { lineStyle: { color: theme.border, opacity: 0.4 } },
    },
    series: [
      {
        type: "line",
        smooth: true,
        showSymbol: false,
        areaStyle: { opacity: 0.12 },
        lineStyle: { width: 2, color: lineColor },
        itemStyle: { color: lineColor },
        data: vals,
      },
    ],
  };
}

export type DeliveryTimeStats = {
  sampleSize: number;
  minHours: number;
  q1Hours: number;
  medianHours: number;
  q3Hours: number;
  maxHours: number;
  outlierCount: number;
  sampleHours: number[];
};

export function buildDeliveryBoxplotOption(d: DeliveryTimeStats, theme: ThemeSlice, hoursLabel: string): EChartsOption {
  if (d.sampleSize === 0) {
    return {
      title: {
        text: hoursLabel,
        left: "center",
        top: "middle",
        textStyle: { color: theme.mutedForeground, fontSize: 13 },
      },
    };
  }
  const boxRow = [d.minHours, d.q1Hours, d.medianHours, d.q3Hours, d.maxHours];
  return {
    tooltip: {
      trigger: "item",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
    },
    xAxis: {
      type: "category",
      data: ["Delivery lead time (h)"],
      axisLabel: { color: theme.mutedForeground },
      axisLine: { lineStyle: { color: theme.border } },
    },
    yAxis: {
      type: "value",
      name: hoursLabel,
      nameTextStyle: { color: theme.mutedForeground },
      axisLabel: { color: theme.mutedForeground },
      splitLine: { lineStyle: { color: theme.border, opacity: 0.4 } },
    },
    series: [
      {
        type: "boxplot",
        data: [boxRow],
        itemStyle: {
          color: "rgba(128,128,128,0.25)",
          borderColor: theme.foreground,
        },
      },
    ],
  };
}

export type HeatmapCell = { dayOfWeek: number; hour: number; count: number };

export function buildDayHourHeatmapOption(
  cells: HeatmapCell[],
  maxCount: number,
  theme: ThemeSlice,
  dayLabels: string[],
): EChartsOption {
  const hours = Array.from({ length: 24 }, (_, i) => String(i));
  const data = cells.map((c) => [c.hour, c.dayOfWeek, c.count] as [number, number, number]);
  return {
    grid: { left: 72, right: 16, top: 16, bottom: 24, containLabel: true },
    tooltip: {
      position: "top",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
    },
    xAxis: { type: "category", data: hours, splitArea: { show: true }, axisLabel: { fontSize: 9, color: theme.mutedForeground } },
    yAxis: { type: "category", data: dayLabels, splitArea: { show: true }, axisLabel: { fontSize: 10, color: theme.mutedForeground } },
    visualMap: {
      min: 0,
      max: Math.max(maxCount, 1),
      calculable: true,
      orient: "horizontal",
      left: "center",
      bottom: 2,
      inRange: { color: ["#f0f0f0", "#3b82f6"] },
      textStyle: { color: theme.mutedForeground, fontSize: 10 },
    },
    series: [
      {
        type: "heatmap",
        data,
        label: { show: false },
        emphasis: { itemStyle: { shadowBlur: 6 } },
      },
    ],
  };
}

/** Bullet-style: today vs 30-day average (vertical bar + horizontal markLine). */
export function buildTodayVsAverageBulletOption(
  today: number,
  avg30: number,
  theme: ThemeSlice,
  todayLabel: string,
  avgLabel: string,
): EChartsOption {
  const cap = Math.max(today, avg30, 1) * 1.2;
  return {
    grid: { left: 48, right: 16, top: 24, bottom: 32, containLabel: true },
    tooltip: {
      trigger: "axis",
      backgroundColor: theme.card,
      borderColor: theme.border,
      textStyle: { color: theme.foreground },
    },
    xAxis: {
      type: "category",
      data: [todayLabel],
      axisLabel: { color: theme.mutedForeground },
    },
    yAxis: {
      type: "value",
      max: cap,
      axisLabel: { color: theme.mutedForeground },
      splitLine: { lineStyle: { color: theme.border, opacity: 0.4 } },
    },
    series: [
      {
        type: "bar",
        data: [today],
        barWidth: 40,
        itemStyle: { color: "var(--chart-1)", borderRadius: [4, 4, 0, 0] },
        markLine: {
          symbol: "none",
          label: { formatter: avgLabel + ": {c}", color: theme.mutedForeground },
          data: [{ yAxis: avg30, name: "avg" }],
          lineStyle: { type: "dashed", color: theme.foreground },
        },
      },
    ],
  };
}
