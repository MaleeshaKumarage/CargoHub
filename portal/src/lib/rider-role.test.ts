import { describe, it, expect } from "vitest";
import { isRiderOnlyPortal } from "./rider-role";

describe("isRiderOnlyPortal", () => {
  it("is true for Rider-only role list", () => {
    expect(isRiderOnlyPortal(["Rider"])).toBe(true);
  });

  it("is false when User is present", () => {
    expect(isRiderOnlyPortal(["Rider", "User"])).toBe(false);
  });

  it("is false for empty or null", () => {
    expect(isRiderOnlyPortal([])).toBe(false);
    expect(isRiderOnlyPortal(null)).toBe(false);
  });
});
