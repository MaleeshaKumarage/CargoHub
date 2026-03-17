import { describe, it, expect, vi } from "vitest";
import { render } from "@/test/test-utils";
import HomeRedirectPage from "./page";

const mockReplace = vi.fn();

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
}));

describe("HomeRedirectPage", () => {
  it("renders null and redirects to dashboard", () => {
    render(<HomeRedirectPage />);
    expect(mockReplace).toHaveBeenCalledWith("/dashboard");
  });
});
