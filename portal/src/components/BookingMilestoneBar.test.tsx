import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { BookingMilestoneBar } from "./BookingMilestoneBar";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

describe("BookingMilestoneBar", () => {
  it("shows draft stage when item is draft only", () => {
    render(<BookingMilestoneBar item={{ isDraft: true }} />);
    const bar = screen.getByRole("progressbar", { name: /milestone/i });
    expect(bar).toHaveAttribute("aria-valuenow", "1");
    expect(bar).toHaveAttribute("aria-valuemin", "1");
    expect(bar).toHaveAttribute("aria-valuemax", "6");
    expect(screen.getByText("draft")).toBeInTheDocument();
  });

  it("shows completed booking stage when not draft and no waybill number", () => {
    render(<BookingMilestoneBar item={{ isDraft: false, waybillNumber: "" }} />);
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "2");
    expect(screen.getByText("completedBooking")).toBeInTheDocument();
  });

  it("shows waybill stage when waybill number is set", () => {
    render(<BookingMilestoneBar item={{ isDraft: false, waybillNumber: "W123" }} />);
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "3");
    expect(screen.getByText("waybill")).toBeInTheDocument();
  });

  it("uses statusHistory when present: two steps completed shows next step 3 and completedBooking label", () => {
    render(
      <BookingMilestoneBar
        item={{
          statusHistory: [
            { status: "Draft", occurredAtUtc: "2025-01-01T00:00:00Z" },
            { status: "CompletedBooking", occurredAtUtc: "2025-01-01T01:00:00Z" },
          ],
        }}
      />
    );
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "3");
    expect(screen.getByText("completedBooking")).toBeInTheDocument();
  });

  it("uses statusHistory when all six milestones completed", () => {
    const statuses = ["Draft", "CompletedBooking", "Waybill", "SendBooking", "Confirmed", "Delivered"];
    render(
      <BookingMilestoneBar
        item={{
          statusHistory: statuses.map((status, i) => ({
            status,
            occurredAtUtc: `2025-01-0${i + 1}T00:00:00Z`,
          })),
        }}
      />
    );
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "6");
    expect(screen.getByText("delivered")).toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <BookingMilestoneBar item={{ isDraft: true }} className="max-w-[200px]" />
    );
    const wrapper = container.firstChild as HTMLElement;
    expect(wrapper.className).toContain("max-w-[200px]");
  });
});
