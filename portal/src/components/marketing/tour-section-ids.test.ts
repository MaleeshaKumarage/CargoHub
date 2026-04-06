import { describe, expect, it } from "vitest";
import { TOUR_SECTION_IDS } from "./tour-section-ids";

describe("TOUR_SECTION_IDS", () => {
  it("includes stable anchor ids for the marketing tour page", () => {
    expect(TOUR_SECTION_IDS).toContain("top");
    expect(TOUR_SECTION_IDS).toContain("platform");
    expect(TOUR_SECTION_IDS).toContain("integrations");
    expect(TOUR_SECTION_IDS).toContain("subscriptions");
    expect(TOUR_SECTION_IDS).toContain("start");
    expect(TOUR_SECTION_IDS.length).toBe(10);
  });

  it("has no duplicate ids", () => {
    const set = new Set(TOUR_SECTION_IDS);
    expect(set.size).toBe(TOUR_SECTION_IDS.length);
  });
});
