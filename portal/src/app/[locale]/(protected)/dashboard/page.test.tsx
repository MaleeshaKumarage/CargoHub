import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import DashboardPage from "./page";

const mockReplace = vi.fn();
const authState = vi.hoisted(() => ({
  roles: ["User"] as string[],
}));
const apiMocks = vi.hoisted(() => ({
  getDashboardStats: vi.fn(),
  adminGetPlatformEarningsSeries: vi.fn().mockResolvedValue([]),
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    user: { displayName: "Test", email: "t@t.com", roles: authState.roles },
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
  useLocale: () => "en",
  useTranslations: (ns: string) => (key: string, values?: Record<string, string | number>) => {
    if (ns === "dashboard") return key in (values ?? {}) ? `${key}:${JSON.stringify(values)}` : key;
    if (ns === "dashboard.stats")
      return values && typeof values === "object" && "week" in values
        ? `${key}:${String(values.week)}`
        : values && typeof values === "object" && "n" in values
          ? `${key}:${values.n}`
          : key;
    if (ns === "dashboard.cards") return key;
    if (ns === "dashboard.earnings") return key;
    return key;
  },
}));

vi.mock("@/lib/api", () => ({
  getDashboardStats: (...args: unknown[]) => apiMocks.getDashboardStats(...args),
  adminGetPlatformEarningsSeries: (...args: unknown[]) => apiMocks.adminGetPlatformEarningsSeries(...args),
}));

vi.mock("@/lib/dashboard-wordcloud", () => ({
  buildWordCloudOption: () => null,
}));

vi.mock("@/components/charts/ThemedECharts", () => ({
  ThemedECharts: () => <div data-testid="themed-echarts-stub" />,
}));

describe("DashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authState.roles = ["User"];
    apiMocks.adminGetPlatformEarningsSeries.mockResolvedValue([]);
    apiMocks.getDashboardStats.mockResolvedValue({
      scope: "all",
      countToday: 5,
      countMonth: 20,
      countYear: 100,
      byCourier: [],
      fromCities: [],
      toCities: [],
      carrierServiceSunburst: null,
      laneSankey: { nodes: [], links: [] },
      bookingsPerDayLast30: [],
      bookingsPerDayCurrentMonth: [],
      kpi: {
        avgPerDayLast30: 0,
        avgPerDayThisMonth: 0,
        avgPerDayThisYear: 0,
        possiblyStuckCount: 0,
      },
      deliveryTime: {
        sampleSize: 0,
        minHours: 0,
        q1Hours: 0,
        medianHours: 0,
        q3Hours: 0,
        maxHours: 0,
        outlierCount: 0,
        sampleHours: [],
      },
      exceptionSignalsHeatmap: { cells: [], maxCount: 0 },
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
    apiMocks.getDashboardStats.mockRejectedValue(new Error("Network error"));
    render(<DashboardPage />);
    await vi.waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("Network error");
    });
  });

  it("loads platform earnings series when SuperAdmin", async () => {
    authState.roles = ["SuperAdmin"];
    apiMocks.adminGetPlatformEarningsSeries.mockResolvedValue([{ period: "2025-01-15", totalEur: 12 }]);
    render(<DashboardPage />);
    await vi.waitFor(() => {
      expect(apiMocks.adminGetPlatformEarningsSeries).toHaveBeenCalledWith("token", "lastMonth", expect.any(AbortSignal));
    });
  });
});
