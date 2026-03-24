import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import BookingsPage from "./page";

const mockReplace = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api", () => ({
  bookingList: vi.fn(),
  draftList: vi.fn(),
  bookingsExportDownload: vi.fn(),
  bookingsImportAnalyze: vi.fn(),
  bookingsImportApplyMapping: vi.fn(),
  bookingsImportConfirm: vi.fn(),
  bookingsImportPreview: vi.fn(),
  bookingsWaybillsBulkDownload: vi.fn(),
}));

const bookingList = await import("@/lib/api").then((m) => m.bookingList);
const draftList = await import("@/lib/api").then((m) => m.draftList);
const bookingsImportPreview = await import("@/lib/api").then((m) => m.bookingsImportPreview);
const bookingsImportConfirm = await import("@/lib/api").then((m) => m.bookingsImportConfirm);
const bookingsWaybillsBulkDownload = await import("@/lib/api").then(
  (m) => m.bookingsWaybillsBulkDownload,
);

describe("BookingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      token: "token",
      isAuthenticated: true,
      isLoading: false,
      user: { roles: ["User"] },
    });
    vi.mocked(bookingList).mockResolvedValue([]);
    vi.mocked(draftList).mockResolvedValue([]);
  });

  it("shows title and Create booking button linking to create", () => {
    render(<BookingsPage />);
    expect(screen.getByRole("heading", { name: /title/i })).toBeInTheDocument();
    const createLink = screen.getByRole("link", { name: /createTitle/i });
    expect(createLink).toHaveAttribute("href", "/bookings/create");
  });

  it("has Completed and Drafts tabs; default is Completed", () => {
    render(<BookingsPage />);
    const completedTab = screen.getByRole("button", { name: /Completed/i });
    const draftsTab = screen.getByRole("button", { name: /Drafts/i });
    expect(completedTab).toBeInTheDocument();
    expect(draftsTab).toBeInTheDocument();
    expect(bookingList).toHaveBeenCalledWith("token");
    expect(draftList).not.toHaveBeenCalled();
  });

  it("switches to Drafts tab and calls draftList", async () => {
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: "Drafts" }));
    await vi.waitFor(() => {
      expect(draftList).toHaveBeenCalledWith("token");
    });
  });

  it("shows loading then empty state when list is empty", async () => {
    render(<BookingsPage />);
    expect(screen.getByText(/Loading…/)).toBeInTheDocument();
    await expect(screen.findByText(/noBookings|Create booking/)).resolves.toBeInTheDocument();
    const createFirst = screen.getByRole("link", { name: /createFirst|Create booking/i });
    expect(createFirst).toHaveAttribute("href", "/bookings/create");
  });

  it("shows error when bookingList fails", async () => {
    vi.mocked(bookingList).mockRejectedValue(new Error("Failed to load bookings"));
    render(<BookingsPage />);
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Failed to load bookings");
  });

  it("shows generic error when rejection is not Error", async () => {
    vi.mocked(bookingList).mockRejectedValue("network error");
    render(<BookingsPage />);
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Failed to load");
  });

  it("renders table with milestone bar when list has items", async () => {
    vi.mocked(bookingList).mockResolvedValue([
      {
        id: "b1",
        shipmentNumber: "SHIP-1",
        customerName: "Acme",
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: true,
        isFavourite: false,
        isDraft: false,
        statusHistory: [{ status: "Draft", occurredAtUtc: "2025-01-01T00:00:00Z" }],
      },
    ]);
    render(<BookingsPage />);
    await expect(screen.findByRole("table")).resolves.toBeInTheDocument();
    expect(screen.getByText("SHIP-1")).toBeInTheDocument();
    expect(screen.getByText("Acme")).toBeInTheDocument();
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /View/i })).toHaveAttribute("href", "/bookings/b1");
  });

  it("drafts tab shows Edit/Confirm link to draft detail", async () => {
    vi.mocked(draftList).mockResolvedValue([
      {
        id: "d1",
        shipmentNumber: null,
        customerName: null,
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: true,
        isFavourite: false,
        isDraft: true,
      },
    ]);
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /Drafts/i }));
    await expect(screen.findByRole("link", { name: /Edit \/ Confirm/i })).resolves.toHaveAttribute(
      "href",
      "/bookings/draft/d1"
    );
  });

  it("SuperAdmin sees no Create button and View link for drafts", async () => {
    mockUseAuth.mockReturnValue({
      token: "token",
      isAuthenticated: true,
      isLoading: false,
      user: { roles: ["SuperAdmin"] },
    });
    vi.mocked(draftList).mockResolvedValue([
      {
        id: "d1",
        shipmentNumber: null,
        customerName: null,
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: true,
        isFavourite: false,
        isDraft: true,
      },
    ]);
    render(<BookingsPage />);
    expect(screen.queryByRole("link", { name: /createTitle/i })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Drafts/i }));
    await expect(screen.findByRole("link", { name: /View/i })).resolves.toHaveAttribute(
      "href",
      "/bookings/draft/d1"
    );
  });

  it("SuperAdmin empty drafts shows no Create booking link", async () => {
    mockUseAuth.mockReturnValue({
      token: "token",
      isAuthenticated: true,
      isLoading: false,
      user: { roles: ["SuperAdmin"] },
    });
    vi.mocked(draftList).mockResolvedValue([]);
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /Drafts/i }));
    await expect(screen.findByText(/No drafts\. Create a booking/)).resolves.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /Create booking/i })).not.toBeInTheDocument();
  });

  it("shows Disabled status for completed booking when enabled is false", async () => {
    vi.mocked(bookingList).mockResolvedValue([
      {
        id: "b1",
        shipmentNumber: "SHIP-1",
        customerName: "Acme",
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: false,
        isFavourite: false,
        isDraft: false,
        statusHistory: [],
      },
    ]);
    render(<BookingsPage />);
    await expect(screen.findByText("filterDisabled")).resolves.toBeInTheDocument();
  });

  it("uses id slice when shipmentNumber is null", async () => {
    vi.mocked(bookingList).mockResolvedValue([
      {
        id: "abc12345-0000",
        shipmentNumber: null,
        customerName: null,
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: true,
        isFavourite: false,
        isDraft: false,
        statusHistory: [],
      },
    ]);
    render(<BookingsPage />);
    await expect(screen.findByText("abc12345")).resolves.toBeInTheDocument();
  });

  it("shows em dash when customerName is null", async () => {
    vi.mocked(bookingList).mockResolvedValue([
      {
        id: "b1",
        shipmentNumber: "SHIP-1",
        customerName: null,
        createdAtUtc: "2025-01-01T00:00:00Z",
        enabled: true,
        isFavourite: false,
        isDraft: false,
        statusHistory: [],
      },
    ]);
    render(<BookingsPage />);
    await expect(screen.findByText("—")).resolves.toBeInTheDocument();
  });

  it("import with zero data rows sets importNoDataRows error", async () => {
    vi.mocked(bookingsImportPreview).mockResolvedValue({
      sessionId: "s0",
      completedCount: 0,
      draftCount: 0,
      skippedEmptyRows: 0,
      totalDataRows: 0,
    });
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /^import$/i }));
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(["h"], "t.csv", { type: "text/csv" })] } });
    await vi.waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("importNoDataRows");
    });
  });

  it("import preview failure shows error on alert", async () => {
    vi.mocked(bookingsImportPreview).mockRejectedValue(new Error("preview failed"));
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /^import$/i }));
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(["h"], "t.csv")] } });
    await vi.waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("preview failed");
    });
  });

  it("import opens dialog and can confirm completed rows", async () => {
    vi.mocked(bookingsImportPreview).mockResolvedValue({
      sessionId: "s1",
      completedCount: 1,
      draftCount: 0,
      skippedEmptyRows: 0,
      totalDataRows: 1,
    });
    vi.mocked(bookingsImportConfirm).mockResolvedValue({
      createdCount: 1,
      draftCount: 0,
      errors: [],
      createdBookingIds: ["bid1"],
      draftBookingIds: [],
    });
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /^import$/i }));
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(["h"], "t.csv")] } });
    await expect(screen.findByRole("dialog")).resolves.toBeInTheDocument();
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: /^importRun$/i }));
    await vi.waitFor(() => {
      expect(bookingsImportConfirm).toHaveBeenCalledWith(
        "token",
        expect.objectContaining({
          sessionId: "s1",
          importCompleted: true,
          importDrafts: false,
        }),
      );
    });
    await expect(screen.findByText("importSummaryLine")).resolves.toBeInTheDocument();
  });

  it("import summary shows errors list and bulk waybill action", async () => {
    vi.mocked(bookingsImportPreview).mockResolvedValue({
      sessionId: "s2",
      completedCount: 1,
      draftCount: 0,
      skippedEmptyRows: 0,
      totalDataRows: 1,
    });
    const errors = Array.from({ length: 9 }, (_, i) => `err-${i}`);
    vi.mocked(bookingsImportConfirm).mockResolvedValue({
      createdCount: 1,
      draftCount: 0,
      errors,
      createdBookingIds: ["b1"],
      draftBookingIds: [],
    });
    vi.mocked(bookingsWaybillsBulkDownload).mockResolvedValue(undefined);
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /^import$/i }));
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(["h"], "t.csv")] } });
    await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: /^importRun$/i }));
    await expect(screen.findByText("importErrors")).resolves.toBeInTheDocument();
    expect(screen.getByText("err-0")).toBeInTheDocument();
    expect(screen.getByText(/\(\+1 more\)/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /^importDownloadWaybills$/i }));
    await vi.waitFor(() => {
      expect(bookingsWaybillsBulkDownload).toHaveBeenCalledWith("token", ["b1"]);
    });
  });

  it("import confirm failure shows alert", async () => {
    vi.mocked(bookingsImportPreview).mockResolvedValue({
      sessionId: "s3",
      completedCount: 1,
      draftCount: 0,
      skippedEmptyRows: 0,
      totalDataRows: 1,
    });
    vi.mocked(bookingsImportConfirm).mockRejectedValue(new Error("confirm failed"));
    render(<BookingsPage />);
    fireEvent.click(screen.getByRole("button", { name: /^import$/i }));
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [new File(["h"], "t.csv")] } });
    await screen.findByRole("dialog");
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: /^importRun$/i }));
    await vi.waitFor(() => {
      expect(screen.getByRole("alert", { hidden: true })).toHaveTextContent("confirm failed");
    });
  });
});
