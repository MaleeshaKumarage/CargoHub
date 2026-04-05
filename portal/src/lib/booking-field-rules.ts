import type { BookingFieldRulesApi, CreateBookingPackage, CreateBookingParty } from "@/lib/api";

export type BookingSectionId =
  | "courier"
  | "shipper"
  | "receiver"
  | "payer"
  | "pickupAddress"
  | "deliveryPoint"
  | "shipment"
  | "shippingInfo"
  | "packages";

export type RequirementLevel = "mandatory" | "optional";

export type BookingFieldRules = {
  version: number;
  sections: Partial<Record<BookingSectionId, RequirementLevel>>;
  fields: Partial<Record<string, RequirementLevel>>;
};

const PARTY_KEYS_FULL = [
  "name",
  "address1",
  "address2",
  "postalCode",
  "city",
  "country",
  "email",
  "phoneNumber",
  "phoneNumberMobile",
  "contactPersonName",
  "vatNo",
  "customerNumber",
] as const;

const PARTY_KEYS_QUICK = ["name", "address1", "postalCode", "city", "country"] as const;

export type PartyFieldKey = (typeof PARTY_KEYS_FULL)[number];

export type BookingRuleFieldDef = {
  sectionId: BookingSectionId;
  fieldId: string;
  /** Key under bookings.fields in messages */
  labelKey: string;
};

const partyDefs = (sectionId: BookingSectionId, prefix: string): BookingRuleFieldDef[] =>
  PARTY_KEYS_FULL.map((k) => ({
    sectionId,
    fieldId: `${prefix}.${k}`,
    labelKey: partyLabelKey(k),
  }));

function partyLabelKey(k: PartyFieldKey): string {
  const map: Record<PartyFieldKey, string> = {
    name: "name",
    address1: "address",
    address2: "address2",
    postalCode: "postalCode",
    city: "city",
    country: "country",
    email: "email",
    phoneNumber: "phone",
    phoneNumberMobile: "mobile",
    contactPersonName: "contactPerson",
    vatNo: "vatNo",
    customerNumber: "customerNumber",
  };
  return map[k];
}

/** All configurable fields in booking create form order (mirrors UI sections). Courier is not configurable here. */
export const BOOKING_RULE_FIELD_DEFINITIONS: BookingRuleFieldDef[] = [
  ...partyDefs("shipper", "shipper"),
  ...partyDefs("receiver", "receiver"),
  ...partyDefs("payer", "payer"),
  ...partyDefs("pickupAddress", "pickupAddress"),
  ...partyDefs("deliveryPoint", "deliveryPoint"),
  { sectionId: "shipment", fieldId: "shipment.service", labelKey: "service" },
  { sectionId: "shipment", fieldId: "shipment.senderReference", labelKey: "senderReference" },
  { sectionId: "shipment", fieldId: "shipment.receiverReference", labelKey: "receiverReference" },
  { sectionId: "shipment", fieldId: "shipment.freightPayer", labelKey: "freightPayer" },
  { sectionId: "shipment", fieldId: "shipment.handlingInstructions", labelKey: "handlingInstructions" },
  { sectionId: "shippingInfo", fieldId: "shippingInfo.grossWeight", labelKey: "grossWeight" },
  { sectionId: "shippingInfo", fieldId: "shippingInfo.grossVolume", labelKey: "grossVolume" },
  { sectionId: "shippingInfo", fieldId: "shippingInfo.packageQuantity", labelKey: "packageQuantity" },
  {
    sectionId: "shippingInfo",
    fieldId: "shippingInfo.pickupHandlingInstructions",
    labelKey: "pickupHandlingInstructions",
  },
  {
    sectionId: "shippingInfo",
    fieldId: "shippingInfo.deliveryHandlingInstructions",
    labelKey: "deliveryHandlingInstructions",
  },
  {
    sectionId: "shippingInfo",
    fieldId: "shippingInfo.generalInstructions",
    labelKey: "generalInstructions",
  },
  { sectionId: "packages", fieldId: "package.weight", labelKey: "weight" },
  { sectionId: "packages", fieldId: "package.volume", labelKey: "volume" },
  { sectionId: "packages", fieldId: "package.packageType", labelKey: "packageType" },
  { sectionId: "packages", fieldId: "package.description", labelKey: "description" },
  { sectionId: "packages", fieldId: "package.length", labelKey: "length" },
  { sectionId: "packages", fieldId: "package.width", labelKey: "width" },
  { sectionId: "packages", fieldId: "package.height", labelKey: "height" },
];

/** Sections shown in Actions → Manage booking (postal/courier is not included). */
export const BOOKING_RULE_SECTION_ORDER: BookingSectionId[] = [
  "shipper",
  "receiver",
  "payer",
  "pickupAddress",
  "deliveryPoint",
  "shipment",
  "shippingInfo",
  "packages",
];

export function defaultBookingFieldRules(): BookingFieldRules {
  return { version: 1, sections: {}, fields: {} };
}

function normLevel(v: string | undefined | null): RequirementLevel {
  const t = (v ?? "").trim().toLowerCase();
  return t === "mandatory" ? "mandatory" : "optional";
}

/** Normalize API payload into a consistent rules object. */
export function parseBookingFieldRulesFromApi(data: unknown): BookingFieldRules {
  if (!data || typeof data !== "object") return defaultBookingFieldRules();
  const o = data as Record<string, unknown>;
  const rawVer = o.version ?? o.Version;
  const version = typeof rawVer === "number" && rawVer > 0 ? rawVer : 1;
  const sections: Partial<Record<BookingSectionId, RequirementLevel>> = {};
  const fields: Partial<Record<string, RequirementLevel>> = {};
  const secRaw = o.sections ?? o.Sections;
  if (secRaw && typeof secRaw === "object") {
    for (const [k, v] of Object.entries(secRaw as Record<string, unknown>)) {
      if (BOOKING_RULE_SECTION_ORDER.includes(k as BookingSectionId))
        sections[k as BookingSectionId] = normLevel(v != null ? String(v) : undefined);
    }
  }
  const fldRaw = o.fields ?? o.Fields;
  if (fldRaw && typeof fldRaw === "object") {
    for (const [k, v] of Object.entries(fldRaw as Record<string, unknown>)) {
      if (k.trim()) fields[k.trim()] = normLevel(v != null ? String(v) : undefined);
    }
  }
  delete sections.courier;
  delete fields["courier.postalService"];
  return { version, sections, fields };
}

export function bookingFieldRulesToApiBody(rules: BookingFieldRules): BookingFieldRulesApi {
  const sections: Record<string, string> = {};
  for (const id of BOOKING_RULE_SECTION_ORDER) {
    sections[id] = rules.sections[id] ?? "optional";
  }
  const fields: Record<string, string> = {};
  for (const def of BOOKING_RULE_FIELD_DEFINITIONS) {
    fields[def.fieldId] = rules.fields[def.fieldId] ?? "optional";
  }
  return { version: rules.version || 1, sections, fields };
}

export function isFieldRequired(sectionId: BookingSectionId, fieldId: string, rules: BookingFieldRules): boolean {
  const secMandatory = normLevel(rules.sections[sectionId]) === "mandatory";
  const fldMandatory = normLevel(rules.fields[fieldId]) === "mandatory";
  return secMandatory || fldMandatory;
}

function isEmpty(value: string | null | undefined): boolean {
  return (value ?? "").trim() === "";
}

function partyKeysVisible(quickBooking: boolean): readonly PartyFieldKey[] {
  return quickBooking ? PARTY_KEYS_QUICK : PARTY_KEYS_FULL;
}

function getPartyValue(party: CreateBookingParty, key: PartyFieldKey): string {
  const v = party[key as keyof CreateBookingParty];
  return v != null ? String(v) : "";
}

export type PayerSource = "sender" | "receiver" | "other";

export type BookingValidationContext = {
  postalService: string;
  shipper: CreateBookingParty;
  receiver: CreateBookingParty;
  payer: CreateBookingParty;
  pickUpAddress: CreateBookingParty;
  deliveryPoint: CreateBookingParty;
  payerSource: PayerSource;
  quickBooking: boolean;
  service: string;
  senderReference: string;
  receiverReference: string;
  freightPayer: string;
  handlingInstructions: string;
  grossWeight: string;
  grossVolume: string;
  packageQuantity: string;
  pickupHandlingInstructions: string;
  deliveryHandlingInstructions: string;
  generalInstructions: string;
  packages: CreateBookingPackage[];
};

/** Field id -> error message. Package keys: `package.${index}.${subkey}` e.g. package.0.weight */
export function validateBookingCreateForm(
  ctx: BookingValidationContext,
  rules: BookingFieldRules,
  requiredMessage: string
): Record<string, string> {
  const errors: Record<string, string> = {};

  const req = (sectionId: BookingSectionId, fieldId: string) =>
    isFieldRequired(sectionId, fieldId, rules);

  const checkParty = (sectionId: BookingSectionId, prefix: string, party: CreateBookingParty) => {
    for (const key of partyKeysVisible(ctx.quickBooking)) {
      const fieldId = `${prefix}.${key}`;
      if (!req(sectionId, fieldId)) continue;
      if (isEmpty(getPartyValue(party, key))) errors[fieldId] = requiredMessage;
    }
  };

  checkParty("shipper", "shipper", ctx.shipper);
  checkParty("receiver", "receiver", ctx.receiver);

  if (ctx.payerSource === "other") {
    checkParty("payer", "payer", ctx.payer);
  }

  checkParty("pickupAddress", "pickupAddress", ctx.pickUpAddress);
  checkParty("deliveryPoint", "deliveryPoint", ctx.deliveryPoint);

  const shipmentFields: { id: string; val: string }[] = [
    { id: "shipment.service", val: ctx.service },
    { id: "shipment.senderReference", val: ctx.senderReference },
    { id: "shipment.receiverReference", val: ctx.receiverReference },
    { id: "shipment.freightPayer", val: ctx.freightPayer },
    { id: "shipment.handlingInstructions", val: ctx.handlingInstructions },
  ];
  for (const { id, val } of shipmentFields) {
    const visible =
      id === "shipment.service" || !ctx.quickBooking;
    if (!visible) continue;
    if (req("shipment", id) && isEmpty(val)) errors[id] = requiredMessage;
  }

  const shipInfo: { id: string; val: string; visible: boolean }[] = [
    { id: "shippingInfo.grossWeight", val: ctx.grossWeight, visible: true },
    { id: "shippingInfo.grossVolume", val: ctx.grossVolume, visible: !ctx.quickBooking },
    { id: "shippingInfo.packageQuantity", val: ctx.packageQuantity, visible: true },
    {
      id: "shippingInfo.pickupHandlingInstructions",
      val: ctx.pickupHandlingInstructions,
      visible: !ctx.quickBooking,
    },
    {
      id: "shippingInfo.deliveryHandlingInstructions",
      val: ctx.deliveryHandlingInstructions,
      visible: !ctx.quickBooking,
    },
    {
      id: "shippingInfo.generalInstructions",
      val: ctx.generalInstructions,
      visible: !ctx.quickBooking,
    },
  ];
  for (const row of shipInfo) {
    if (!row.visible) continue;
    if (req("shippingInfo", row.id) && isEmpty(row.val)) errors[row.id] = requiredMessage;
  }

  const pkgFieldVisible = (sub: string): boolean => {
    if (sub === "weight" || sub === "packageType") return true;
    return !ctx.quickBooking;
  };

  ctx.packages.forEach((pkg, index) => {
    const subKeys: (keyof CreateBookingPackage)[] = [
      "weight",
      "volume",
      "packageType",
      "description",
      "length",
      "width",
      "height",
    ];
    for (const sub of subKeys) {
      if (!pkgFieldVisible(sub)) continue;
      const fieldId = `package.${sub}`;
      if (!req("packages", fieldId)) continue;
      const val = pkg[sub];
      if (isEmpty(val != null ? String(val) : "")) {
        errors[`package.${index}.${String(sub)}`] = requiredMessage;
      }
    }
  });

  return errors;
}

export function defsForSection(sectionId: BookingSectionId): BookingRuleFieldDef[] {
  return BOOKING_RULE_FIELD_DEFINITIONS.filter((d) => d.sectionId === sectionId);
}

/** When a whole section is set to mandatory, every field in that section is set to mandatory in draft state. */
export function applySectionRequirement(
  prev: BookingFieldRules,
  sectionId: BookingSectionId,
  level: RequirementLevel
): BookingFieldRules {
  const sections = { ...prev.sections, [sectionId]: level };
  const fields = { ...prev.fields };
  if (level === "mandatory") {
    for (const def of defsForSection(sectionId)) {
      fields[def.fieldId] = "mandatory";
    }
  }
  return { ...prev, sections, fields };
}
