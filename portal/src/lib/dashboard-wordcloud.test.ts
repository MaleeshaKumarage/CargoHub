import { describe, it, expect, vi } from "vitest";

vi.mock("echarts-wordcloud", () => ({}));

import { buildWordCloudOption } from "./dashboard-wordcloud";

const theme = {
  border: "#ccc",
  foreground: "#111",
  mutedForeground: "#666",
  card: "#fff",
};

describe("buildWordCloudOption", () => {
  it("returns null for empty rows", () => {
    expect(buildWordCloudOption([], ["#a"], theme, "bookings")).toBeNull();
  });

  it("builds wordCloud series with colors and tooltip formatter", () => {
    const opt = buildWordCloudOption(
      [
        { key: "A", count: 3 },
        { key: "B", count: 2 },
      ],
      ["#f00", "#0f0"],
      theme,
      "bookings",
    );
    expect(opt).not.toBeNull();
    const series = opt!.series?.[0] as {
      type: string;
      data: { name: string; value: number; textStyle: { color: string } }[];
    };
    expect(series?.type).toBe("wordCloud");
    expect(series?.data[0]).toMatchObject({
      name: "A",
      value: 3,
      textStyle: { color: "#f00" },
    });
    expect(series?.data[1].textStyle.color).toBe("#0f0");
    const fmt = opt!.tooltip?.formatter as (p: unknown) => string;
    expect(fmt({ name: "X", value: 9 })).toContain("X");
    expect(fmt({ name: "X", value: 9 })).toContain("9");
    expect(fmt({})).toContain("bookings");
  });
});
