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
const mockGetBreakdownByRange = vi.fn();
const mockGetDetail = vi.fn();
const mockGetUsers = vi.fn();
const mockDownloadPdf = vi.fn();
const mockSendInvoiceEmail = vi.fn();
const mockSetBookingExcluded = vi.fn();

vi.mock("@/lib/api", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api")>("@/lib/api");
  return {
    ...actual,
    adminGetCompanies: (...a: unknown[]) => mockGetCompanies(...a),
    adminGetBillingBreakdownByDateRange: (...a: unknown[]) => mockGetBreakdownByRange(...a),
    adminGetBillingPeriodDetail: (...a: unknown[]) => mockGetDetail(...a),
    adminGetUsers: (...a: unknown[]) => mockGetUsers(...a),
    adminDownloadBillingPeriodInvoicePdf: (...a: unknown[]) => mockDownloadPdf(...a),
    adminSendBillingPeriodInvoiceEmail: (...a: unknown[]) => mockSendInvoiceEmail(...a),
    adminSetBookingInvoiceExcluded: (...a: unknown[]) => mockSetBookingExcluded(...a),
  };
});

const defaultBreakdown = {
  companyId: "c1",
  yearUtc: 2026,
  monthUtc: 4,
  billingPeriodId: "per1" as string | null,
  rangeStartUtc: "2026-04-01T00:00:00Z",
  rangeEndExclusiveUtc: "2026-05-01T00:00:00Z",
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
    mockDownloadPdf.mockResolvedValue(new Blob());
    mockSendInvoiceEmail.mockResolvedValue(undefined);
    mockSetBookingExcluded.mockResolvedValue(undefined);
    mockGetBreakdownByRange.mockResolvedValue(defaultBreakdown);
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

  async function selectCompanyAndLoadBreakdown() {
    const companyCombo = screen.getByRole("combobox", { name: "selectCompany" });
    await waitFor(() => expect(companyCombo).not.toBeDisabled());
    fireEvent.change(companyCombo, { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByRole("button", { name: "loadBreakdown" })).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "loadBreakdown" }));
    await waitFor(() => expect(mockGetBreakdownByRange).toHaveBeenCalledWith("jwt", "c1", expect.any(String), expect.any(String)));
    await waitFor(() => expect(screen.queryByText("loadingBreakdown")).not.toBeInTheDocument());
  }

  it("renders heading and loads companies", async () => {
    render(<ManageInvoicesPage />);
    expect(screen.getByRole("heading", { name: "title" })).toBeInTheDocument();
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalledWith("jwt"));
  });

  it("loads breakdown when company selected and load clicked; opens line detail", async () => {
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    await waitFor(() => expect(screen.getByRole("button", { name: "viewLines" })).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(mockGetDetail).toHaveBeenCalledWith("jwt", "per1"));
    await waitFor(() => expect(screen.getByText("PerBooking")).toBeInTheDocument());
  });

  it("shows error when breakdown request fails", async () => {
    mockGetBreakdownByRange.mockRejectedValueOnce(new Error("breakdown failed"));
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    const companyCombo = screen.getByRole("combobox", { name: "selectCompany" });
    await waitFor(() => expect(companyCombo).not.toBeDisabled());
    fireEvent.change(companyCombo, { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByRole("button", { name: "loadBreakdown" })).toBeInTheDocument());
    fireEvent.click(screen.getByRole("button", { name: "loadBreakdown" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent(/breakdown failed/));
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
    await selectCompanyAndLoadBreakdown();
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
    await selectCompanyAndLoadBreakdown();
    fireEvent.click(screen.getByRole("button", { name: "viewLines" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent(/detail failed/));
  });

  it("shows no bookings in range when breakdown has zero bookings", async () => {
    mockGetBreakdownByRange.mockResolvedValueOnce({
      ...defaultBreakdown,
      billableBookingCount: 0,
      bookings: [],
      payableTotal: 0,
      ledgerTotal: 0,
    });
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    await waitFor(() => expect(screen.getByText("noBookingsInRange")).toBeInTheDocument());
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
    await selectCompanyAndLoadBreakdown();
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

  it("shows endBeforeStart when range end is before start", async () => {
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    const companyCombo = screen.getByRole("combobox", { name: "selectCompany" });
    await waitFor(() => expect(companyCombo).not.toBeDisabled());
    fireEvent.change(companyCombo, { target: { value: "c1" } });
    await waitFor(() => expect(screen.getByLabelText("periodStart")).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText("periodStart"), { target: { value: "2026-04-10" } });
    fireEvent.change(screen.getByLabelText("periodEnd"), { target: { value: "2026-04-01" } });
    fireEvent.click(screen.getByRole("button", { name: "loadBreakdown" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("endBeforeStart"));
  });

  it("shows multi-month invoice hint when period id is missing but bookings exist", async () => {
    mockGetBreakdownByRange.mockResolvedValueOnce({
      ...defaultBreakdown,
      billingPeriodId: null,
      billableBookingCount: 1,
    });
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    await waitFor(() => expect(screen.getByText("multiMonthInvoiceHint")).toBeInTheDocument());
  });

  it("downloads PDF when save invoice is clicked", async () => {
    mockDownloadPdf.mockResolvedValueOnce(new Blob(["%PDF"], { type: "application/pdf" }));
    const origCreate = URL.createObjectURL?.bind(URL);
    const origRevoke = URL.revokeObjectURL?.bind(URL);
    URL.createObjectURL = vi.fn(() => "blob:mock");
    URL.revokeObjectURL = vi.fn();
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    fireEvent.click(screen.getByRole("button", { name: "saveInvoice" }));
    await waitFor(() =>
      expect(mockDownloadPdf).toHaveBeenCalledWith("jwt", "per1", { from: expect.any(String), to: expect.any(String) }),
    );
    if (origCreate) URL.createObjectURL = origCreate;
    else delete (URL as unknown as { createObjectURL?: unknown }).createObjectURL;
    if (origRevoke) URL.revokeObjectURL = origRevoke;
    else delete (URL as unknown as { revokeObjectURL?: unknown }).revokeObjectURL;
  });

  it("sends invoice email when recipient selected", async () => {
    mockSendInvoiceEmail.mockResolvedValueOnce(undefined);
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    const recipient = screen.getByLabelText("recipientAdmin");
    await waitFor(() => expect(recipient).not.toBeDisabled());
    fireEvent.change(recipient, { target: { value: "u1" } });
    fireEvent.click(screen.getByRole("button", { name: "sendInvoice" }));
    await waitFor(() =>
      expect(mockSendInvoiceEmail).toHaveBeenCalledWith("jwt", "per1", "u1", {
        from: expect.any(String),
        to: expect.any(String),
      }),
    );
  });

  it("toggles exclude booking and refreshes breakdown", async () => {
    mockSetBookingExcluded.mockResolvedValue(undefined);
    render(<ManageInvoicesPage />);
    await waitFor(() => expect(mockGetCompanies).toHaveBeenCalled());
    await selectCompanyAndLoadBreakdown();
    const sw = screen.getAllByRole("switch", { name: "excludeBooking" })[0];
    fireEvent.click(sw);
    await waitFor(() =>
      expect(mockSetBookingExcluded).toHaveBeenCalledWith("jwt", "per1", "b1", true),
    );
    await waitFor(() => expect(mockGetBreakdownByRange).toHaveBeenCalledTimes(2));
  });
});
