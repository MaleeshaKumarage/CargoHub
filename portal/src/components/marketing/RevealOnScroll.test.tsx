import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { act, render, screen } from "@/test/test-utils";
import { RevealOnScroll } from "./RevealOnScroll";

describe("RevealOnScroll", () => {
  let callback: IntersectionObserverCallback | null = null;

  beforeEach(() => {
    callback = null;
    vi.stubGlobal(
      "IntersectionObserver",
      vi.fn(function MockIntersectionObserver(cb: IntersectionObserverCallback) {
        callback = cb;
        return {
          observe: vi.fn(),
          disconnect: vi.fn(),
          unobserve: vi.fn(),
          takeRecords: () => [],
          root: null,
          rootMargin: "",
          thresholds: [],
        } as unknown as IntersectionObserver;
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders children and applies visible styles after intersection", () => {
    render(
      <RevealOnScroll>
        <p>Tour content</p>
      </RevealOnScroll>,
    );
    const inner = screen.getByText("Tour content");
    const root = inner.parentElement;
    expect(root).toBeTruthy();
    expect(root?.className).toMatch(/opacity-0/);

    act(() => {
      callback?.(
        [{ isIntersecting: true } as IntersectionObserverEntry],
        {} as IntersectionObserver,
      );
    });

    expect(root?.className).toMatch(/opacity-100/);
  });
});
