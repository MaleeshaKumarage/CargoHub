import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import BookingDetailPage from "./page";

const mockPush = vi.fn();
const mockReplace = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    token: "token",
    isAuthenticated: true,
    isLoading: false,
  }),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ push: mockPush, replace: mockReplace }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "booking-123" }),
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("@/lib/api", () => ({
  bookingGet: vi.fn(),
  getWaybillPdfBlobUrl: vi.fn(),
}));

vi.mock("@/components/BookingMilestoneBar", () => ({
  BookingMilestoneBar: ({ item }: { item: { isDraft?: boolean } }) => (
    <div data-testid="milestone-bar" data-draft={item.isDraft}>
      Milestone
    </div>
  ),
}));

const bookingGet = await import("@/lib/api").then((m) => m.bookingGet);
const getWaybillPdfBlobUrl = await import("@/lib/api").then((m) => m.getWaybillPdfBlobUrl);

const mockBooking = {
  id: "booking-123",
  customerId: "cust",
  shipmentNumber: "SHIP-1",
  waybillNumber: "W-001",
  customerName: "Acme",
  enabled: true,
  isTestBooking: false,
  isFavourite: false,
  isDraft: false,
  createdAtUtc: "2025-01-01T00:00:00Z",
  updatedAtUtc: "2025-01-01T00:00:00Z",
  header: {
    senderId: "s1",
    companyId: null,
    referenceNumber: "REF-1",
    postalService: "Posti",
  },
  receiver: {
    name: "Receiver Co",
    address1: "Street 1",
    postalCode: "00100",
    city: "Helsinki",
    country: "FI",
  },
  shipper: null,
  payer: null,
  pickUpAddress: null,
  deliveryPoint: null,
  shipment: null,
  shippingInfo: null,
  packages: [],
};

describe("BookingDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(bookingGet).mockResolvedValue(mockBooking);
    vi.mocked(getWaybillPdfBlobUrl).mockResolvedValue("blob:https://example.com/waybill.pdf");
  });

  it("shows loading then booking header and milestone bar", async () => {
    render(<BookingDetailPage />);
    expect(screen.getByText(/Loading…/)).toBeInTheDocument();
    await expect(screen.findByRole("heading", { name: /Booking/ })).resolves.toBeInTheDocument();
    expect(screen.getByTestId("milestone-bar")).toBeInTheDocument();
  });

  it("shows Print waybill button for completed booking and calls getWaybillPdfBlobUrl on click", async () => {
    const openSpy = vi.spyOn(window, "open").mockImplementation(() => null);
    render(<BookingDetailPage />);
    const printBtn = await screen.findByRole("button", { name: "Print waybill" });
    fireEvent.click(printBtn);
    await vi.waitFor(() => {
      expect(getWaybillPdfBlobUrl).toHaveBeenCalledWith("token", "booking-123");
    });
    expect(openSpy).toHaveBeenCalledWith("blob:https://example.com/waybill.pdf", "_blank", "noopener");
    openSpy.mockRestore();
  });

  it("does not show Print waybill for draft", async () => {
    vi.mocked(bookingGet).mockResolvedValue({ ...mockBooking, isDraft: true });
    render(<BookingDetailPage />);
    await screen.findByRole("heading", { name: /Booking/ });
    expect(screen.queryByRole("button", { name: /Print waybill/i })).not.toBeInTheDocument();
  });

  it("shows error when bookingGet fails", async () => {
    vi.mocked(bookingGet).mockRejectedValue(new Error("Booking not found"));
    render(<BookingDetailPage />);
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Booking not found");
  });

  it("shows error when getWaybillPdfBlobUrl fails on Print waybill click", async () => {
    vi.mocked(getWaybillPdfBlobUrl).mockRejectedValue(new Error("Waybill not ready"));
    render(<BookingDetailPage />);
    await screen.findByRole("button", { name: /Print waybill/i });
    fireEvent.click(screen.getByRole("button", { name: /Print waybill/i }));
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Waybill not ready");
  });
});
