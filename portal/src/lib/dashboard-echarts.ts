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
