import { describe, expect, it } from "vitest";
import { TOUR_DEMO_CHART_COLORS, TOUR_DEMO_ECHARTS_THEME } from "./tour-demo-theme";

describe("tour-demo-theme", () => {
  it("provides a theme slice for ECharts builders", () => {
    expect(TOUR_DEMO_ECHARTS_THEME.card).toMatch(/oklch/);
    expect(TOUR_DEMO_ECHARTS_THEME.foreground.length).toBeGreaterThan(3);
  });

  it("has five chart colors", () => {
    expect(TOUR_DEMO_CHART_COLORS).toHaveLength(5);
  });
});
