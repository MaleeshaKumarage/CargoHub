import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import {
  TourAdminPreview,
  TourBookingPreview,
  TourCarriersPreview,
  TourDashboardPreview,
  TourWorkflowPreview,
} from "./TourRealPreviews";

vi.mock("@/components/charts/ThemedECharts", () => ({
  ThemedECharts: () => <div data-testid="themed-echarts" />,
}));

vi.mock("next-intl", () => ({
  useTranslations: (namespace?: string) => (key: string) =>
    namespace ? `${namespace}.${key}` : key,
}));

describe("TourRealPreviews", () => {
  it("renders booking preview with milestone and field labels", () => {
    render(<TourBookingPreview />);
    expect(screen.getByText("bookings.createTitle")).toBeInTheDocument();
    expect(screen.getByLabelText("bookings.fields.referenceNumber")).toBeInTheDocument();
    expect(screen.getByText("milestones.draft")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "home.createBooking" })).toBeInTheDocument();
  });

  it("renders dashboard preview with chart placeholders", () => {
    render(<TourDashboardPreview />);
    expect(screen.getAllByTestId("themed-echarts").length).toBeGreaterThanOrEqual(3);
    expect(screen.getByText("dashboard.stats.last7DaysTitle")).toBeInTheDocument();
  });

  it("renders workflow preview tiles", () => {
    render(<TourWorkflowPreview />);
    expect(screen.getAllByText("nav.dashboard").length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText("nav.bookings").length).toBeGreaterThanOrEqual(1);
  });

  it("renders carriers preview with disclaimer", () => {
    render(<TourCarriersPreview />);
    expect(screen.getByText("nav.courierContracts")).toBeInTheDocument();
    expect(screen.getByText("tour.previewDisclaimer")).toBeInTheDocument();
  });

  it("renders admin preview with status labels", () => {
    render(<TourAdminPreview />);
    expect(screen.getByText("tour.previewAdminNote")).toBeInTheDocument();
    expect(screen.getByText("tour.previewActive")).toBeInTheDocument();
    expect(screen.getAllByText("tour.previewTrial").length).toBe(2);
  });
});
