import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import BookingsPage from "./page";

const mockReplace = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
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
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api", () => ({
  bookingList: vi.fn(),
  draftList: vi.fn(),
}));

const bookingList = await import("@/lib/api").then((m) => m.bookingList);
const draftList = await import("@/lib/api").then((m) => m.draftList);

describe("BookingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
});
