import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@/test/test-utils";
import ManageInvoicesPage from "./page";

const mockUseAuth = vi.fn();

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => mockUseAuth(),
}));

const mockGetCompanies = vi.fn();
const mockGetBillableMonths = vi.fn();
const mockGetBreakdown = vi.fn();
const mockGetDetail = vi.fn();
const mockGetUsers = vi.fn();

vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api")>("@/lib/api");
  return {
    ...actual,
    adminGetCompanies: (...a: unknown[]) => mockGetCompanies(...a),
    adminGetBillableMonths: (...a: unknown[]) => mockGetBillableMonths(...a),
    adminGetBillingMonthBreakdown: (...a: unknown[]) => mockGetBreakdown(...a),
    adminGetBillingPeriodDetail: (...a: unknown[]) => mockGetDetail(...a),
    adminGetUsers: (...a: unknown[]) => mockGetUsers(...a),
    adminDownloadBillingPeriodInvoicePdf: vi.fn(),
    adminSendBillingPeriodInvoiceEmail: vi.fn(),
    adminSetBookingInvoiceExcluded: vi.fn(),
  };
});

const defaultBreakdown = {
  companyId: "c1",
  yearUtc: 2026,
  monthUtc: 4,
  billingPeriodId: "per1",
  currency: "EUR",
  billableBookingCount: 1,
  payableTotal: 10,
  ledgerTotal: 10,
  segments: [] as { label: string; bookingCount: number; unitRate: number | null; subtotal: number; planKind: string; subscriptionPlanId: string | null }[],
  bookings: [
    {
      bookingId: "b1",
      shipmentNumber: null as string | null,
      referenceNumber: "REF1",
      planLabel: "Plan A",
      description: "d1",
      amount: 10,
      excludedFromInvoice: false,
    },
  ],
};

describe("ManageInvoicesPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ token: "jwt" });
    mockGetUsers.mockResolvedValue([
      { userId: "u1", email: "a@x.com", displayName: "", businessId: "123", isActive: true, roles: ["Admin"] },
    ]);
    mockGetCompanies.mockResolvedValue([
      { id: "c1", name: "Acme", companyId: "x", businessId: "123" },
    ]);
    mockGetBillableMonths.mockResolvedValue([
      { yearUtc: 2026, monthUtc: 4, billableBookingCount: 1, billingPeriodId: "per1" },
    ]);
    mockGetBreakdown.mockResolvedValue(defaultBreakdown);
    mockGetDetail.mockResolvedValue({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 10,
      lineItems: [
        {
          id: "l1",
          bookingId: null,
          lineType: "PerBooking",
          component: null,
          amount: 10,
          currency: "EUR",
          excludedFromInvoice: false,
          createdAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
    });
  });

  async function selectCompanyAndMonth() {
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(mockGetBillableMonths).toHaveBeenCalledWith("jwt", "c1"));
    await waitFor(() => expect(screen.getByRole("combobox", { name: "billableMonth" })).toBeInTheDocument());
    fireEvent.change(screen.getByRole("combobox", { name: "billableMonth" }), {
      target: { value: "2026-04" },
    });
    await waitFor(() => expect(mockGetBreakdown).toHaveBeenCalledWith("jwt", "c1", 2026, 4));
  }

  it("renders heading and loads companies", async () => {
    render(<ManageInvoicesPage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalledWith("jwt"));
  });

  it("loads breakdown when company and month selected and opens line detail", async () => {
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndMonth();
    await waitFor(() => expect(screen.getByRole("button", { name: "viewLines" })).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(mockGetDetail).toHaveBeenCalledWith("jwt", "per1"));
    await waitFor(() => expect(screen.getByText("PerBooking")).toBeInTheDocument());
  });

  it("shows error when billable months request fails", async () => {
    mockGetBillableMonths.mockRejectedValueOnce(new Error("months failed"));
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent(/months failed/));
  });

  it("shows excluded marker text for invoice-excluded lines in detail dialog", async () => {
    mockGetDetail.mockResolvedValueOnce({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 0,
      lineItems: [
        {
          id: "l1",
          bookingId: null,
          lineType: "Adjustment",
          component: null,
          amount: 1,
          currency: "EUR",
          excludedFromInvoice: true,
          createdAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
    });
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndMonth();
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByText("excludedYes")).toBeInTheDocument());
  });

  it("renders nothing when token is missing", () => {
    mockUseAuth.mockReturnValue({ token: null });
    const { container } = render(<ManageInvoicesPage />);
    expect(container.firstChild).toBeNull();
  });

  it("shows error when billing period detail fails", async () => {
    mockGetDetail.mockRejectedValueOnce(new Error("detail failed"));
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndMonth();
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent(/detail failed/));
  });

  it("shows no billable months message when list is empty", async () => {
    mockGetBillableMonths.mockResolvedValueOnce([]);
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    fireEvent.change(screen.getByRole("combobox", { name: "selectCompany" }), { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByText("noBillableMonths")).toBeInTheDocument());
  });

  it("shows dialog loading then detail after fetch resolves", async () => {
    let resolveDetail!: (value: {
      id: string;
      companyId: string;
      yearUtc: number;
      monthUtc: number;
      currency: string;
      status: string;
      payableTotal: number;
      lineItems: Array<{
        id: string;
        bookingId: string | null;
        lineType: string;
        component: string | null;
        amount: number;
        currency: string;
        excludedFromInvoice: boolean;
        createdAtUtc: string;
      }>;
    }) => void;
    mockGetDetail.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          resolveDetail = resolve;
        })
    );
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndMonth();
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByText("…")).toBeInTheDocument());
    resolveDetail({
      id: "per1",
      companyId: "c1",
      yearUtc: 2026,
      monthUtc: 4,
      currency: "EUR",
      status: "Open",
      payableTotal: 0,
      lineItems: [],
    });
    await waitFor(() => expect(screen.queryByText("…")).not.toBeInTheDocument());
  });
});
