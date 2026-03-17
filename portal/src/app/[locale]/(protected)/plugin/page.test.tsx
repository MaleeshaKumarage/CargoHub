import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import PluginPage from "./page";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

describe("PluginPage", () => {
  it("renders title and card", () => {
    render(<PluginPage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    expect(screen.getAllByText("description")).toHaveLength(2);
    expect(screen.getByText("comingSoon")).toBeInTheDocument();
  });
});
