import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import ActionsPage from "./page";

const mockReplace = vi.fn();

vi.mock("@/context/AuthContext", () => ({
  useAuth: () => ({
    token: "token",
    user: { roles: ["User"] },
    isAuthenticated: true,
    isLoading: false,
  }),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ replace: mockReplace }),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api", () => ({
  getAddressBook: vi.fn(),
  addSender: vi.fn(),
  addReceiver: vi.fn(),
}));

const getAddressBook = await import("@/lib/api").then((m) => m.getAddressBook);
const addSender = await import("@/lib/api").then((m) => m.addSender);
const addReceiver = await import("@/lib/api").then((m) => m.addReceiver);

const addressBookResponse = {
  addressBooks: [
    {
      companyId: "c1",
      companyName: "Test Co",
      senders: [{ id: "s1", name: "Sender A", city: "Helsinki" }],
      receivers: [{ id: "r1", name: "Receiver B", city: "Tampere" }],
    },
  ],
};

describe("ActionsPage (Address book)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getAddressBook).mockResolvedValue(addressBookResponse);
  });

  it("shows title and address book card", async () => {
    render(<ActionsPage />);
    expect(screen.getByRole("heading", { name: /title/i })).toBeInTheDocument();
    await expect(screen.findByText("addressBook")).resolves.toBeInTheDocument();
  });

  it("loads address book and shows senders and receivers tables", async () => {
    render(<ActionsPage />);
    expect(getAddressBook).toHaveBeenCalledWith("token");
    await expect(screen.findByText("Sender A")).resolves.toBeInTheDocument();
    expect(screen.getByText("Receiver B")).toBeInTheDocument();
    expect(screen.getByText("senders")).toBeInTheDocument();
    expect(screen.getByText("receivers")).toBeInTheDocument();
  });

  it("shows loading then content", async () => {
    render(<ActionsPage />);
    expect(screen.getByText("loading")).toBeInTheDocument();
    await expect(screen.findByText("Sender A")).resolves.toBeInTheDocument();
  });

  it("shows error when getAddressBook fails", async () => {
    vi.mocked(getAddressBook).mockRejectedValue(new Error("Company not found"));
    render(<ActionsPage />);
    await expect(screen.findByRole("alert")).resolves.toHaveTextContent("Company not found");
  });

  it("shows add sender and add receiver forms with submit buttons", async () => {
    vi.mocked(getAddressBook).mockResolvedValue(addressBookResponse);
    render(<ActionsPage />);
    await screen.findByText("Sender A");
    expect(screen.getByRole("button", { name: "addSender" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "addReceiver" })).toBeInTheDocument();
  });

  it("shows sender and receiver tables with data", async () => {
    vi.mocked(getAddressBook).mockResolvedValue(addressBookResponse);
    render(<ActionsPage />);
    await screen.findByText("Sender A");
    expect(screen.getByText("Receiver B")).toBeInTheDocument();
    expect(screen.getByText("Helsinki")).toBeInTheDocument();
    expect(screen.getByText("Tampere")).toBeInTheDocument();
  });

  it("shows no company message when address book is empty", async () => {
    vi.mocked(getAddressBook).mockResolvedValue({ addressBooks: [] });
    render(<ActionsPage />);
    await expect(screen.findByText("noCompany")).resolves.toBeInTheDocument();
  });
});
