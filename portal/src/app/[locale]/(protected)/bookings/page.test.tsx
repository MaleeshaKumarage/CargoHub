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
  bookingsImportPreview: vi.fn(),
  bookingsImportConfirm: vi.fn(),
  bookingsWaybillsBulkDownload: vi.fn(),
}));

const bookingList = await import("@/lib/api").then((m) => m.bookingList);
const draftList = await import("@/lib/api").then((m) => m.draftList);

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
});
