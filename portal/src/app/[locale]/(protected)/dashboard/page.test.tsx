import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import DashboardPage from "./page";

const mockReplace = vi.fn();
const mockGetDashboardStats = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    user: { displayName: "Test", email: "t@t.com", roles: ["User"] },
    token: "token",
    isAuthenticated: true,
    isLoading: false,
  }),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: (ns: string) => (key: string, values?: Record<string, string>) => {
    if (ns === "dashboard") return key in (values ?? {}) ? `${key}:${JSON.stringify(values)}` : key;
    if (ns === "dashboard.stats") return key;
    if (ns === "dashboard.cards") return key;
    return key;
  },
}));

vi.mock("@/lib/api", () => ({
  getDashboardStats: (...args: unknown[]) => mockGetDashboardStats(...args),
}));

describe("DashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDashboardStats.mockResolvedValue({
      countToday: 5,
      countMonth: 20,
      countYear: 100,
      byCourier: [],
      fromCities: [],
      toCities: [],
    });
  });

  it("renders greeting and stats when authenticated", async () => {
    render(<DashboardPage />);
    await vi.waitFor(() => {
      expect(screen.getByText(/greeting/)).toBeInTheDocument();
    });
    await vi.waitFor(async () => {
      const fives = await screen.findAllByText("5");
      const twenties = await screen.findAllByText("20");
      const hundreds = await screen.findAllByText("100");
      expect(fives.length).toBeGreaterThan(0);
      expect(twenties.length).toBeGreaterThan(0);
      expect(hundreds.length).toBeGreaterThan(0);
    });
  });

  it("shows create booking card for non-SuperAdmin", async () => {
    render(<DashboardPage />);
    await vi.waitFor(() => {
      expect(screen.getByRole("link", { name: /createBooking/ })).toBeInTheDocument();
    });
  });

  it("shows stats error when getDashboardStats fails", async () => {
    mockGetDashboardStats.mockRejectedValueOnce(new Error("Network error"));
    render(<DashboardPage />);
    await vi.waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("Network error");
    });
  });
});
