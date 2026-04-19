import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor, act } from "@/test/test-utils";
import { FreelanceRiderSection } from "./FreelanceRiderSection";

const getMatches = vi.fn();

vi.mock("@/lib/api", () => ({
  getFreelanceRiderMatches: (...args: unknown[]) => getMatches(...args),
}));

vi.mock("next-intl", () => {
  const t = (key: string) => key;
  return {
    useTranslations: () => t,
  };
});

describe("FreelanceRiderSection", () => {
  beforeEach(() => {
    getMatches.mockReset();
  });

  it("loads matches after debounce when postals are long enough", async () => {
    getMatches.mockResolvedValue([{ id: "r1", displayName: "Rider One", email: "a@b.com" }]);

    render(
      <FreelanceRiderSection
        token="t"
        shipperPostal="00100"
        receiverPostal="00200"
        value=""
        onChange={() => {}}
        idPrefix="t"
      />,
    );

    await waitFor(
      () => {
        expect(getMatches).toHaveBeenCalledWith("t", {
          shipperPostal: "00100",
          receiverPostal: "00200",
          companyId: undefined,
        });
      },
      { timeout: 3000 },
    );

    expect(await screen.findByRole("combobox")).toBeInTheDocument();
  });

  it("does not fetch when shipper postal is too short", async () => {
    vi.useFakeTimers();
    render(
      <FreelanceRiderSection
        token="t"
        shipperPostal="01"
        receiverPostal="00200"
        value=""
        onChange={() => {}}
      />,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(500);
    });

    expect(getMatches).not.toHaveBeenCalled();
    vi.useRealTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });
});
