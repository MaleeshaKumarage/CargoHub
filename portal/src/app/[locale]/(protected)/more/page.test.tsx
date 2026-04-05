import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
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

const mockGetCompanySubscription = vi.fn();
vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api")>("@/lib/api");
  return {
    ...actual,
    getCompanySubscription: (...args: unknown[]) => mockGetCompanySubscription(...args),
  };
});

describe("MorePage", () => {
  beforeEach(() => {
    mockUseAuth.mockClear();
    mockGetCompanySubscription.mockReset();
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
    mockUseAuth.mockReturnValue({ user: { email: "u@x.com", businessId: "1234567-8" }, token: null });
    render(<MorePage />);
    expect(screen.getByText(/companyLabel/)).toBeInTheDocument();
    expect(screen.getByText(/1234567-8/)).toBeInTheDocument();
  });

  it("loads and shows subscription name and pricing when token and businessId exist", async () => {
    mockGetCompanySubscription.mockResolvedValue({
      planName: "Trial",
      planKind: "Trial",
      currency: "EUR",
      trialBookingAllowance: 5,
    });
    mockUseAuth.mockReturnValue({
      user: { email: "u@x.com", businessId: "BIZ-1" },
      token: "jwt",
    });
    render(<MorePage />);
    await waitFor(() => {
      expect(mockGetCompanySubscription).toHaveBeenCalledWith("jwt");
    });
    await waitFor(() => {
      expect(screen.getByText("subscriptionTitle")).toBeInTheDocument();
      expect(screen.getByText("Trial")).toBeInTheDocument();
      expect(screen.getByText("subscriptionPlanLabel")).toBeInTheDocument();
      expect(screen.getByText("subscriptionPricingLabel")).toBeInTheDocument();
    });
  });
});
