import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@/test/test-utils";
import { BookingsDataTable } from "./BookingsDataTable";
import type { BookingListItem } from "@/lib/api";

vi.mock("@/components/ui/dropdown-menu", () => import("@/test/mock-dropdown-menu"));

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

const baseRow = (over: Partial<BookingListItem> = {}): BookingListItem => ({
  id: "id-1",
  shipmentNumber: "SN-1",
  customerName: "Acme",
  createdAtUtc: "2025-06-01T12:00:00Z",
  enabled: true,
  isFavourite: false,
  isDraft: false,
  statusHistory: [],
  ...over,
});

describe("BookingsDataTable", () => {
  const onExport = vi.fn().mockResolvedValue(undefined);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls onExport with csv and xlsx from export menu", async () => {
    render(
      <BookingsDataTable
        data={[baseRow()]}
        isDrafts={false}
        isSuperAdmin={false}
        exportLoading={false}
        onExport={onExport}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /export/i }));
    fireEvent.click(await screen.findByRole("menuitem", { name: /exportCsv/i }));
    expect(onExport).toHaveBeenCalledWith("csv");
    fireEvent.click(screen.getByRole("button", { name: /export/i }));
    fireEvent.click(await screen.findByRole("menuitem", { name: /exportExcel/i }));
    expect(onExport).toHaveBeenCalledWith("xlsx");
  });

  it("shows Exporting when exportLoading", () => {
    render(
      <BookingsDataTable
        data={[baseRow()]}
        isDrafts={false}
        isSuperAdmin={false}
        exportLoading
        onExport={onExport}
      />,
    );
    expect(screen.getByRole("button", { name: /Exporting/i })).toBeDisabled();
  });

  it("does not render export menu for drafts", () => {
    render(
      <BookingsDataTable
        data={[baseRow({ isDraft: true })]}
        isDrafts
        isSuperAdmin={false}
        exportLoading={false}
        onExport={onExport}
      />,
    );
    expect(screen.queryByRole("button", { name: /export/i })).not.toBeInTheDocument();
  });

  it("cycles sort indicators on reference column", () => {
    render(
      <BookingsDataTable
        data={[baseRow(), baseRow({ id: "id-2", shipmentNumber: "SN-2", customerName: "Beta" })]}
        isDrafts={false}
        isSuperAdmin={false}
        exportLoading={false}
        onExport={onExport}
      />,
    );
    const refBtn = screen.getByRole("button", { name: /tableReference/i });
    fireEvent.click(refBtn);
    fireEvent.click(refBtn);
    fireEvent.click(refBtn);
  });

  it("filters rows by search input", async () => {
    render(
      <BookingsDataTable
        data={[
          baseRow({ shipmentNumber: "ALPHA-1", customerName: "X" }),
          baseRow({ id: "b2", shipmentNumber: "BETA-2", customerName: "Y" }),
        ]}
        isDrafts={false}
        isSuperAdmin={false}
        exportLoading={false}
        onExport={onExport}
      />,
    );
    const input = screen.getByPlaceholderText("filterSearch");
    fireEvent.change(input, { target: { value: "beta" } });
    expect(screen.getByText("BETA-2")).toBeInTheDocument();
    expect(screen.queryByText("ALPHA-1")).not.toBeInTheDocument();
  });

  it("shows noResults when filter matches nothing", () => {
    render(
      <BookingsDataTable
        data={[baseRow()]}
        isDrafts={false}
        isSuperAdmin={false}
        exportLoading={false}
        onExport={onExport}
      />,
    );
    fireEvent.change(screen.getByPlaceholderText("filterSearch"), {
      target: { value: "zzznomatch" },
    });
    expect(screen.getByText("noResults")).toBeInTheDocument();
  });
});
