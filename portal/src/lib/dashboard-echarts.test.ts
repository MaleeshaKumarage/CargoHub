import { describe, it, expect } from "vitest";
import {
  buildPeriodBarOption,
  buildCourierPieOption,
  buildCityBarOption,
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
    const series = opt.series?.[0];
    expect(series?.type).toBe("bar");
    const data = series?.data as { value: number; itemStyle: { color: string } }[];
    expect(data[0].value).toBe(3);
    expect(data[0].itemStyle.color).toBe("#a00");
    expect(data[1].value).toBe(7);
    expect(opt.xAxis).toMatchObject({ type: "category", data: ["A", "B"] });
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
    const s = opt.series?.[0];
    expect(s?.type).toBe("pie");
    const data = s?.data as { name: string; value: number; itemStyle: { color: string } }[];
    expect(data[0]).toMatchObject({ name: "DHL", value: 10, itemStyle: { color: "#c1" } });
    expect(data[1]).toMatchObject({ name: "Posti", value: 5, itemStyle: { color: "#c2" } });
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
