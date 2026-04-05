import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import CreateBookingPage from "./page";

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    token: "token",
    user: { roles: ["User", "Admin"] },
    isAuthenticated: true,
    isLoading: false,
  }),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api", () => ({
  getMe: vi.fn().mockResolvedValue({ companyName: "Test Co", roles: [] }),
  getAddressBook: vi.fn(),
  getBookingFieldRules: vi.fn().mockResolvedValue({ version: 1, sections: {}, fields: {} }),
  getCouriers: vi.fn(),
  bookingCreate: vi.fn(),
  draftCreate: vi.fn(),
  addSender: vi.fn(),
  addReceiver: vi.fn(),
}));

const getAddressBook = await import("@/lib/api").then((m) => m.getAddressBook);
const getCouriers = await import("@/lib/api").then((m) => m.getCouriers);
const bookingCreate = await import("@/lib/api").then((m) => m.bookingCreate);

describe("CreateBookingPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getAddressBook).mockResolvedValue({
      addressBooks: [{ companyId: "c1", companyName: "Co", senders: [], receivers: [] }],
    });
    vi.mocked(getCouriers).mockResolvedValue(["DHLExpress"]);
  });

  it("loads address book and shows create form with createTitle button", async () => {
    render(<CreateBookingPage />);
    expect(getAddressBook).toHaveBeenCalledWith("token");
    await expect(
      screen.findByRole("button", { name: /createTitle/i })
    ).resolves.toBeInTheDocument();
  });

  it("shows createTitle heading and saveAsDraft button", async () => {
    render(<CreateBookingPage />);
    await screen.findByRole("button", { name: /createTitle/i });
    expect(screen.getByRole("heading", { name: /createTitle/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /saveAsDraft/i })).toBeInTheDocument();
  });

  it("shows no couriers banner and disables submit when courier list is empty", async () => {
    vi.mocked(getCouriers).mockResolvedValue([]);
    render(<CreateBookingPage />);
    await expect(screen.findByRole("status")).resolves.toHaveTextContent(/noCouriersBannerTitle/);
    expect(screen.getByRole("button", { name: /createTitle/i })).toBeDisabled();
    expect(screen.getByRole("button", { name: /saveAsDraft/i })).toBeDisabled();
  });

  it("shows error when bookingCreate fails", async () => {
    vi.mocked(bookingCreate).mockRejectedValue(new Error("Validation failed"));
    render(<CreateBookingPage />);
    await screen.findByRole("button", { name: /createTitle/i });
    fireEvent.click(screen.getByRole("button", { name: /createTitle/i }));
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Validation failed");
  });
});
