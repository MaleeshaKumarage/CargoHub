import type { EChartsOption } from "echarts";
import "echarts-wordcloud";
import type { KeyCount, ThemeSlice } from "./dashboard-echarts";

/**
 * Word cloud for carriers or cities (Apache ECharts + echarts-wordcloud).
 * Kept in a separate module so Vitest can import `dashboard-echarts` without canvas.
 */
export function buildWordCloudOption(
  rows: KeyCount[],
  seriesColors: string[],
  theme: ThemeSlice,
  bookingsLabel: string,
): EChartsOption | null {
  if (!rows.length) return null;
  const data = rows.slice(0, 80).map((r, i) => ({
    name: r.key,
    value: r.count,
    textStyle: { color: seriesColors[i % seriesColors.length] },
  }));
  return {
    tooltip: {
      show: true,
      backgroundColor: theme.card,
      borderColor: theme.border,
      borderWidth: 1,
      textStyle: { color: theme.foreground },
      formatter: (p: unknown) => {
        const x = p as { name?: string; value?: number };
        return `${x.name ?? ""}<br/>${x.value ?? 0} ${bookingsLabel}`;
      },
    },
    series: [
      {
        type: "wordCloud",
        shape: "circle",
        left: "center",
        top: "center",
        width: "92%",
        height: "92%",
        sizeRange: [12, 44],
        rotationRange: [-45, 45],
        gridSize: 8,
        drawOutOfBound: false,
        data,
      },
    ] as EChartsOption["series"],
  };
}
