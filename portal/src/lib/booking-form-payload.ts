import type { CreateBookingBody, CreateBookingPackage, CreateBookingParty } from "@/lib/api";
import type { PayerSource } from "@/lib/booking-field-rules";

export function trimOrUndefined(s: string) {
  const t = s?.trim();
  return t === "" ? undefined : t;
}

export function defaultParty(): CreateBookingParty {
  return {
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
  };
}

export function defaultPackage(): CreateBookingPackage {
  return {
    weight: "",
    volume: "",
    packageType: "",
    description: "",
    length: "",
    width: "",
    height: "",
  };
}

export function toPartyPayload(p: CreateBookingParty): CreateBookingParty | undefined {
  const has =
    p.name ||
    p.address1 ||
    p.postalCode ||
    p.city ||
    p.country ||
    p.email ||
    p.phoneNumber ||
    p.contactPersonName ||
    p.vatNo ||
    p.customerNumber;
  if (!has) return undefined;
  return {
    name: trimOrUndefined(p.name ?? ""),
    address1: trimOrUndefined(p.address1 ?? ""),
    address2: trimOrUndefined(p.address2 ?? ""),
    postalCode: trimOrUndefined(p.postalCode ?? ""),
    city: trimOrUndefined(p.city ?? ""),
    country: trimOrUndefined(p.country ?? "") || "FI",
    email: trimOrUndefined(p.email ?? ""),
    phoneNumber: trimOrUndefined(p.phoneNumber ?? ""),
    phoneNumberMobile: trimOrUndefined(p.phoneNumberMobile ?? ""),
    contactPersonName: trimOrUndefined(p.contactPersonName ?? ""),
    vatNo: trimOrUndefined(p.vatNo ?? ""),
    customerNumber: trimOrUndefined(p.customerNumber ?? ""),
  };
}

export type BuildCreateBookingBodyInput = {
  referenceNumber: string;
  postalService: string;
  companyId: string;
  receiver: CreateBookingParty;
  shipper: CreateBookingParty;
  payer: CreateBookingParty;
  pickUpAddress: CreateBookingParty;
  deliveryPoint: CreateBookingParty;
  payerSource: PayerSource;
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
  deliveryWithoutSignature: boolean;
  packages: CreateBookingPackage[];
};

/** Shared by create booking and draft update — matches backend CreateBookingRequest shape. */
export function buildCreateBookingBody(input: BuildCreateBookingBodyInput): CreateBookingBody {
  const {
    referenceNumber,
    postalService,
    companyId,
    receiver,
    shipper,
    payer,
    pickUpAddress,
    deliveryPoint,
    payerSource,
    service,
    senderReference,
    receiverReference,
    freightPayer,
    handlingInstructions,
    grossWeight,
    grossVolume,
    packageQuantity,
    pickupHandlingInstructions,
    deliveryHandlingInstructions,
    generalInstructions,
    deliveryWithoutSignature,
    packages,
  } = input;

  return {
    referenceNumber: trimOrUndefined(referenceNumber),
    postalService: trimOrUndefined(postalService),
    companyId: trimOrUndefined(companyId),
    receiverName: trimOrUndefined(receiver.name ?? ""),
    receiverAddress1: trimOrUndefined(receiver.address1 ?? ""),
    receiverAddress2: trimOrUndefined(receiver.address2 ?? ""),
    receiverPostalCode: trimOrUndefined(receiver.postalCode ?? ""),
    receiverCity: trimOrUndefined(receiver.city ?? ""),
    receiverCountry: trimOrUndefined(receiver.country ?? "") || "FI",
    receiverEmail: trimOrUndefined(receiver.email ?? ""),
    receiverPhone: trimOrUndefined(receiver.phoneNumber ?? ""),
    receiverPhoneMobile: trimOrUndefined(receiver.phoneNumberMobile ?? ""),
    receiverContactPersonName: trimOrUndefined(receiver.contactPersonName ?? ""),
    receiverVatNo: trimOrUndefined(receiver.vatNo ?? ""),
    receiverCustomerNumber: trimOrUndefined(receiver.customerNumber ?? ""),
    shipper: toPartyPayload(shipper),
    payer:
      payerSource === "sender"
        ? toPartyPayload(shipper)
        : payerSource === "receiver"
          ? toPartyPayload(receiver)
          : toPartyPayload(payer),
    pickUpAddress: toPartyPayload(pickUpAddress),
    deliveryPoint: toPartyPayload(deliveryPoint),
    shipment:
      service || senderReference || receiverReference || freightPayer || handlingInstructions
        ? {
            service: trimOrUndefined(service),
            senderReference: trimOrUndefined(senderReference),
            receiverReference: trimOrUndefined(receiverReference),
            freightPayer: trimOrUndefined(freightPayer),
            handlingInstructions: trimOrUndefined(handlingInstructions),
          }
        : undefined,
    shippingInfo:
      grossWeight ||
      grossVolume ||
      packageQuantity ||
      pickupHandlingInstructions ||
      deliveryHandlingInstructions ||
      generalInstructions ||
      packages.some((p) => p.weight || p.volume || p.packageType || p.description)
        ? {
            grossWeight: trimOrUndefined(grossWeight),
            grossVolume: trimOrUndefined(grossVolume),
            packageQuantity:
              trimOrUndefined(packageQuantity) || (packages.length > 0 ? String(packages.length) : undefined),
            pickupHandlingInstructions: trimOrUndefined(pickupHandlingInstructions),
            deliveryHandlingInstructions: trimOrUndefined(deliveryHandlingInstructions),
            generalInstructions: trimOrUndefined(generalInstructions),
            deliveryWithoutSignature,
            packages: packages
              .map((p) => ({
                weight: trimOrUndefined(p.weight ?? ""),
                volume: trimOrUndefined(p.volume ?? ""),
                packageType: trimOrUndefined(p.packageType ?? ""),
                description: trimOrUndefined(p.description ?? ""),
                length: trimOrUndefined(p.length ?? ""),
                width: trimOrUndefined(p.width ?? ""),
                height: trimOrUndefined(p.height ?? ""),
              }))
              .filter((p) => p.weight || p.volume || p.packageType || p.description),
          }
        : undefined,
  };
}
