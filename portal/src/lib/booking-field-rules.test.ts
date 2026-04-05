import { describe, expect, it } from "vitest";
import {
  bookingFieldRulesToApiBody,
  defaultBookingFieldRules,
  isFieldRequired,
  parseBookingFieldRulesFromApi,
  validateBookingCreateForm,
} from "./booking-field-rules";
import type { CreateBookingPackage, CreateBookingParty } from "./api";

const party = (over: Partial<CreateBookingParty> = {}): CreateBookingParty => ({
  name: "",
  address1: "",
  address2: "",
  postalCode: "",
  city: "",
  country: "FI",
  email: "",
  phoneNumber: "",
  phoneNumberMobile: "",
  contactPersonName: "",
  vatNo: "",
  customerNumber: "",
  ...over,
});

const pkg = (over: Partial<CreateBookingPackage> = {}): CreateBookingPackage => ({
  weight: "",
  volume: "",
  packageType: "",
  description: "",
  length: "",
  width: "",
  height: "",
  ...over,
});

describe("parseBookingFieldRulesFromApi", () => {
  it("returns defaults for null", () => {
    const r = parseBookingFieldRulesFromApi(null);
    expect(r.version).toBe(1);
    expect(Object.keys(r.sections).length).toBe(0);
  });

  it("reads sections and fields case-insensitively", () => {
    const r = parseBookingFieldRulesFromApi({
      Version: 1,
      Sections: { shipper: "MANDATORY" },
      Fields: { "shipper.name": "optional" },
    });
    expect(r.sections.shipper).toBe("mandatory");
    expect(r.fields["shipper.name"]).toBe("optional");
  });
});

describe("isFieldRequired", () => {
  it("is true when section is mandatory", () => {
    const rules = defaultBookingFieldRules();
    rules.sections.shipper = "mandatory";
    expect(isFieldRequired("shipper", "shipper.email", rules)).toBe(true);
  });

  it("is true when field is mandatory", () => {
    const rules = defaultBookingFieldRules();
    rules.fields["shipment.service"] = "mandatory";
    expect(isFieldRequired("shipment", "shipment.service", rules)).toBe(true);
  });

  it("is false when both optional", () => {
    const rules = defaultBookingFieldRules();
    expect(isFieldRequired("receiver", "receiver.name", rules)).toBe(false);
  });
});

describe("validateBookingCreateForm", () => {
  const baseCtx = () => ({
    postalService: "Posti",
    shipper: party({ name: "S", address1: "A", postalCode: "1", city: "C", country: "FI" }),
    receiver: party({ name: "R", address1: "B", postalCode: "2", city: "D", country: "FI" }),
    payer: party(),
    pickUpAddress: party(),
    deliveryPoint: party(),
    payerSource: "sender" as const,
    quickBooking: false,
    service: "x",
    senderReference: "",
    receiverReference: "",
    freightPayer: "",
    handlingInstructions: "",
    grossWeight: "1",
    grossVolume: "",
    packageQuantity: "1",
    pickupHandlingInstructions: "",
    deliveryHandlingInstructions: "",
    generalInstructions: "",
    packages: [pkg({ weight: "1", packageType: "box" })],
  });

  it("flags empty postal service when courier mandatory", () => {
    const rules = defaultBookingFieldRules();
    rules.sections.courier = "mandatory";
    const ctx = { ...baseCtx(), postalService: "" };
    const err = validateBookingCreateForm(ctx, rules, "Required");
    expect(err["courier.postalService"]).toBe("Required");
  });

  it("skips payer party when payer is sender", () => {
    const rules = defaultBookingFieldRules();
    rules.sections.payer = "mandatory";
    const ctx = { ...baseCtx(), payerSource: "sender" as const, payer: party() };
    const err = validateBookingCreateForm(ctx, rules, "Required");
    expect(err["payer.name"]).toBeUndefined();
  });

  it("in quick booking does not require hidden shipper fields", () => {
    const rules = defaultBookingFieldRules();
    rules.fields["shipper.email"] = "mandatory";
    const ctx = { ...baseCtx(), quickBooking: true, shipper: party({ name: "n", address1: "a", postalCode: "p", city: "c", country: "FI" }) };
    const err = validateBookingCreateForm(ctx, rules, "Required");
    expect(err["shipper.email"]).toBeUndefined();
  });

  it("validates package rows with indexed keys", () => {
    const rules = defaultBookingFieldRules();
    rules.fields["package.weight"] = "mandatory";
    const ctx = { ...baseCtx(), packages: [pkg({ weight: "", packageType: "x" })] };
    const err = validateBookingCreateForm(ctx, rules, "Req");
    expect(err["package.0.weight"]).toBe("Req");
  });
});

describe("bookingFieldRulesToApiBody", () => {
  it("includes every section and field with default optional", () => {
    const body = bookingFieldRulesToApiBody(defaultBookingFieldRules());
    expect(body.version).toBe(1);
    expect(body.sections.shipper).toBe("optional");
    expect(body.fields["shipper.name"]).toBe("optional");
  });
});
