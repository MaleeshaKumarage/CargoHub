import type { ThemeSlice } from "@/lib/dashboard-echarts";

/**
 * Static theme slice for tour ECharts (canvas cannot read CSS vars from a nested preview box).
 * Aligned with `.dark[data-theme="minimalism"]` chart/card tokens.
 */
export const TOUR_DEMO_ECHARTS_THEME: ThemeSlice = {
  border: "oklch(1 0 0 / 10%)",
  foreground: "oklch(0.95 0.005 264)",
  mutedForeground: "oklch(0.65 0.02 264)",
  card: "oklch(0.2 0.015 264)",
};

/** Series colors matching dashboard chart-1 … chart-5 (minimalism dark). */
export const TOUR_DEMO_CHART_COLORS = [
  "oklch(0.55 0.15 264)",
  "oklch(0.6 0.12 200)",
  "oklch(0.65 0.1 160)",
  "oklch(0.6 0.15 300)",
  "oklch(0.55 0.18 30)",
] as const;
