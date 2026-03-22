"use client";

import { useEffect, useState } from "react";

export type ChartTheme = {
  chart: [string, string, string, string, string];
  border: string;
  foreground: string;
  mutedForeground: string;
  background: string;
  card: string;
};

const FALLBACK: ChartTheme = {
  chart: ["#888888", "#888888", "#888888", "#888888", "#888888"],
  border: "#cccccc",
  foreground: "#000000",
  mutedForeground: "#666666",
  background: "#ffffff",
  card: "#ffffff",
};

function readTheme(): ChartTheme {
  const root = document.documentElement;
  const g = (name: string) => getComputedStyle(root).getPropertyValue(name).trim();
  const c = (n: string) => g(n) || FALLBACK.chart[0];
  return {
    chart: [c("--chart-1"), c("--chart-2"), c("--chart-3"), c("--chart-4"), c("--chart-5")],
    border: g("--border") || FALLBACK.border,
    foreground: g("--foreground") || FALLBACK.foreground,
    mutedForeground: g("--muted-foreground") || FALLBACK.mutedForeground,
    background: g("--background") || FALLBACK.background,
    card: g("--card") || FALLBACK.card,
  };
}

/**
 * Reads design-token colors from the document for ECharts (canvas cannot use CSS variables directly).
 * Updates when the root `class` changes (e.g. dark mode) or on window resize.
 */
export function useChartTheme(): ChartTheme {
  const [theme, setTheme] = useState<ChartTheme>(FALLBACK);

  useEffect(() => {
    const sync = () => setTheme(readTheme());
    sync();
    const el = document.documentElement;
    const obs = new MutationObserver(sync);
    obs.observe(el, { attributes: true, attributeFilter: ["class"] });
    window.addEventListener("resize", sync);
    return () => {
      obs.disconnect();
      window.removeEventListener("resize", sync);
    };
  }, []);

  return theme;
}
