import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useChartTheme } from "./use-chart-theme";

describe("useChartTheme", () => {
  beforeEach(() => {
    vi.spyOn(window, "getComputedStyle").mockImplementation((el: Element) => {
      if (el === document.documentElement) {
        return {
          getPropertyValue: (name: string) => {
            const map: Record<string, string> = {
              "--chart-1": "#111111",
              "--chart-2": "#222222",
              "--chart-3": "#333333",
              "--chart-4": "#444444",
              "--chart-5": "#555555",
              "--border": "#aaaaaa",
              "--foreground": "#000000",
              "--muted-foreground": "#666666",
              "--background": "#ffffff",
              "--card": "#eeeeee",
            };
            return map[name] ?? "";
          },
        } as CSSStyleDeclaration;
      }
      return {
        getPropertyValue: () => "",
      } as CSSStyleDeclaration;
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("reads chart CSS variables from the document after mount", async () => {
    const { result } = renderHook(() => useChartTheme());

    await waitFor(() => {
      expect(result.current.chart[0]).toBe("#111111");
    });
    expect(result.current.border).toBe("#aaaaaa");
    expect(result.current.card).toBe("#eeeeee");
  });
});
