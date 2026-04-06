import { describe, it, expect } from "vitest";
import { render } from "@/test/test-utils";
import { TechGridBackground } from "./TechGridBackground";

describe("TechGridBackground", () => {
  it("renders fixed decorative layers", () => {
    const { container } = render(<TechGridBackground />);
    const root = container.firstChild as HTMLElement;
    expect(root).toHaveAttribute("aria-hidden", "true");
    expect(root.querySelectorAll("div").length).toBeGreaterThanOrEqual(3);
  });
});
