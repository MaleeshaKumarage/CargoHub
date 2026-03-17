import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import MorePage from "./page";

const mockUseAuth = vi.fn();

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children }: { children: React.ReactNode }) => <a href="#">{children}</a>,
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => mockUseAuth(),
}));

describe("MorePage", () => {
  beforeEach(() => {
    mockUseAuth.mockClear();
  });

  it("renders title and signed in info with email", () => {
    mockUseAuth.mockReturnValue({ user: { email: "u@example.com", displayName: "User" } });
    render(<MorePage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    expect(screen.getByText("u@example.com")).toBeInTheDocument();
  });

  it("renders with displayName when email is missing", () => {
    mockUseAuth.mockReturnValue({ user: { displayName: "Test User" } });
    render(<MorePage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    expect(screen.getByText("Test User")).toBeInTheDocument();
  });

  it("renders dash when user has no email or displayName", () => {
    mockUseAuth.mockReturnValue({ user: {} });
    render(<MorePage />);
    expect(screen.getByText("—")).toBeInTheDocument();
  });

  it("renders company businessId when present", () => {
    mockUseAuth.mockReturnValue({ user: { email: "u@x.com", businessId: "1234567-8" } });
    render(<MorePage />);
    expect(screen.getByText(/Company: 1234567-8/)).toBeInTheDocument();
  });
});
