import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { ThemedECharts } from "./ThemedECharts";

vi.mock("echarts-for-react", () => ({
  default: function MockEcharts({
    style,
    option,
  }: {
    style?: React.CSSProperties;
    option: { series?: unknown };
  }) {
    return (
      <div
        data-testid="echarts-mock"
        data-height={style?.height}
        data-has-series={option?.series != null ? "1" : "0"}
      />
    );
  },
}));

describe("ThemedECharts", () => {
  it("renders echarts-for-react with height and option", () => {
    render(
      <ThemedECharts
        option={{ series: [{ type: "bar", data: [1] }] }}
        height={200}
      />,
    );
    const el = screen.getByTestId("echarts-mock");
    expect(el).toHaveAttribute("data-height", "200");
    expect(el).toHaveAttribute("data-has-series", "1");
  });
});
