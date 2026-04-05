"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useState, useEffect } from "react";
import {
  bookingCreate,
  draftCreate,
  getAddressBook,
  getBookingFieldRules,
  getCouriers,
  getMe,
  addSender,
  addReceiver,
  type CreateBookingBody,
  type CreateBookingParty,
  type CreateBookingPackage,
  type AddressBookResponse,
  type AddressEntry,
} from "@/lib/api";
import {
  defaultBookingFieldRules,
  parseBookingFieldRulesFromApi,
  validateBookingCreateForm,
  type BookingFieldRules,
} from "@/lib/booking-field-rules";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const defaultParty = (): CreateBookingParty => ({
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
});

const defaultPackage = (): CreateBookingPackage => ({
  weight: "",
  volume: "",
  packageType: "",
  description: "",
  length: "",
  width: "",
  height: "",
});

type PayerSource = "sender" | "receiver" | "other";

function trimOrUndefined(s: string) {
  const t = s?.trim();
  return t === "" ? undefined : t;
}

function PartyFields({
  title,
  state,
  setState,
  quickBooking,
  fieldKeyPrefix,
  fieldErrors,
}: {
  title: string;
  state: CreateBookingParty;
  setState: (p: CreateBookingParty) => void;
  quickBooking?: boolean;
  fieldKeyPrefix: string;
  fieldErrors?: Record<string, string>;
}) {
  const f = useTranslations("bookings.fields");
  const tBookings = useTranslations("bookings");
  const optional = tBookings("optional");
  const update = (key: keyof CreateBookingParty, value: string) => setState({ ...state, [key]: value });
  const errs = fieldErrors ?? {};
  const err = (key: string) => errs[`${fieldKeyPrefix}.${key}`];
  return (
    <div className="space-y-3">
      {title ? <h4 className="font-medium text-sm">{title}</h4> : null}
      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-1">
          <Label>{f("name")}</Label>
          <Input value={state.name ?? ""} onChange={(e) => update("name", e.target.value)} placeholder={f("namePlaceholder")} />
          {err("name") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("name")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("address")}</Label>
          <Input value={state.address1 ?? ""} onChange={(e) => update("address1", e.target.value)} placeholder={f("addressPlaceholder")} />
          {err("address1") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("address1")}
            </p>
          ) : null}
        </div>
        {!quickBooking && (
          <div className="space-y-1">
            <Label>{f("address2")}</Label>
            <Input value={state.address2 ?? ""} onChange={(e) => update("address2", e.target.value)} placeholder={optional} />
            {err("address2") ? (
              <p className="text-xs text-destructive" role="alert">
                {err("address2")}
              </p>
            ) : null}
          </div>
        )}
        <div className="space-y-1">
          <Label>{f("postalCode")}</Label>
          <Input value={state.postalCode ?? ""} onChange={(e) => update("postalCode", e.target.value)} placeholder={f("postalCodePlaceholder")} />
          {err("postalCode") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("postalCode")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("city")}</Label>
          <Input value={state.city ?? ""} onChange={(e) => update("city", e.target.value)} placeholder={f("cityPlaceholder")} />
          {err("city") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("city")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("country")}</Label>
          <Input value={state.country ?? ""} onChange={(e) => update("country", e.target.value)} placeholder={f("countryPlaceholder")} />
          {err("country") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("country")}
            </p>
          ) : null}
        </div>
        {!quickBooking && (
          <>
            <div className="space-y-1">
              <Label>{f("email")}</Label>
              <Input type="email" value={state.email ?? ""} onChange={(e) => update("email", e.target.value)} placeholder={optional} />
              {err("email") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("email")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("phone")}</Label>
              <Input value={state.phoneNumber ?? ""} onChange={(e) => update("phoneNumber", e.target.value)} placeholder={optional} />
              {err("phoneNumber") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("phoneNumber")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("mobile")}</Label>
              <Input value={state.phoneNumberMobile ?? ""} onChange={(e) => update("phoneNumberMobile", e.target.value)} placeholder={optional} />
              {err("phoneNumberMobile") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("phoneNumberMobile")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("contactPerson")}</Label>
              <Input value={state.contactPersonName ?? ""} onChange={(e) => update("contactPersonName", e.target.value)} placeholder={optional} />
              {err("contactPersonName") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("contactPersonName")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("vatNo")}</Label>
              <Input value={state.vatNo ?? ""} onChange={(e) => update("vatNo", e.target.value)} placeholder={optional} />
              {err("vatNo") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("vatNo")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("customerNumber")}</Label>
              <Input value={state.customerNumber ?? ""} onChange={(e) => update("customerNumber", e.target.value)} placeholder={optional} />
              {err("customerNumber") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("customerNumber")}
                </p>
              ) : null}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

export default function CreateBookingPage() {
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");
  const isAdmin = Array.isArray(user?.roles) && user.roles.includes("Admin");
  const t = useTranslations("bookings");
  const tSections = useTranslations("bookings.sections");
  const tFields = useTranslations("bookings.fields");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [postalService, setPostalService] = useState("");
  const [companyId, setCompanyId] = useState("");
  const [receiver, setReceiver] = useState(defaultParty);
  const [shipper, setShipper] = useState(defaultParty);
  const [payer, setPayer] = useState(defaultParty);
  const [pickUpAddress, setPickUpAddress] = useState(defaultParty);
  const [deliveryPoint, setDeliveryPoint] = useState(defaultParty);
  const [service, setService] = useState("");
  const [senderReference, setSenderReference] = useState("");
  const [receiverReference, setReceiverReference] = useState("");
  const [freightPayer, setFreightPayer] = useState("");
  const [handlingInstructions, setHandlingInstructions] = useState("");
  const [grossWeight, setGrossWeight] = useState("");
  const [grossVolume, setGrossVolume] = useState("");
  const [packageQuantity, setPackageQuantity] = useState("");
  const [pickupHandlingInstructions, setPickupHandlingInstructions] = useState("");
  const [deliveryHandlingInstructions, setDeliveryHandlingInstructions] = useState("");
  const [generalInstructions, setGeneralInstructions] = useState("");
  const [deliveryWithoutSignature, setDeliveryWithoutSignature] = useState(false);
  const [packages, setPackages] = useState<CreateBookingPackage[]>([defaultPackage()]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [quickBooking, setQuickBooking] = useState(false);
  const [payerSource, setPayerSource] = useState<PayerSource>("other");
  const [addressBook, setAddressBook] = useState<AddressBookResponse | null>(null);
  const [courierIds, setCourierIds] = useState<string[]>([]);
  const [couriersReady, setCouriersReady] = useState(false);
  const [saveToAddressBook, setSaveToAddressBook] = useState(true);
  const [bookingRules, setBookingRules] = useState<BookingFieldRules>(() => defaultBookingFieldRules());
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!token) return;
    getAddressBook(token).then((res) => setAddressBook(res.addressBooks?.[0] ?? null)).catch(() => setAddressBook(null));
  }, [token]);

  useEffect(() => {
    if (!token) return;
    getBookingFieldRules(token)
      .then((api) => setBookingRules(parseBookingFieldRulesFromApi(api)))
      .catch(() => setBookingRules(defaultBookingFieldRules()));
  }, [token]);

  useEffect(() => {
    if (!token) return;
    setCouriersReady(false);
    getCouriers(token)
      .then(setCourierIds)
      .catch(() => setCourierIds([]))
      .finally(() => setCouriersReady(true));
  }, [token]);

  useEffect(() => {
    if (!token) return;
    getMe(token)
      .then((me) => {
        setReferenceNumber(me.companyName ?? "");
        setCompanyId(me.businessId ?? "");
      })
      .catch(() => {});
  }, [token]);

  function addressToParty(a: AddressEntry): CreateBookingParty {
    return {
      name: a.name ?? "",
      address1: a.address1 ?? "",
      address2: a.address2 ?? "",
      postalCode: a.postalCode ?? "",
      city: a.city ?? "",
      country: a.country ?? "FI",
      email: a.email ?? "",
      phoneNumber: a.phoneNumber ?? "",
      phoneNumberMobile: "",
      contactPersonName: a.contactPersonName ?? "",
      vatNo: a.vatNo ?? "",
      customerNumber: a.customerNumber ?? "",
    };
  }

  function partyToAddressEntry(p: CreateBookingParty): AddressEntry {
    return {
      name: p.name || undefined,
      address1: p.address1 || undefined,
      address2: p.address2 || undefined,
      postalCode: p.postalCode || undefined,
      city: p.city || undefined,
      country: p.country || "FI",
      email: p.email || undefined,
      phoneNumber: p.phoneNumber || undefined,
      contactPersonName: p.contactPersonName || undefined,
      vatNo: p.vatNo || undefined,
      customerNumber: p.customerNumber || undefined,
    };
  }

  function partyExistsInAddressList(party: CreateBookingParty, list: AddressEntry[]): boolean {
    const n = (s: string | null | undefined) => (s ?? "").trim();
    const nCountry = (s: string | null | undefined) => n(s) || "FI";
    const pName = n(party.name);
    const pAddr = n(party.address1);
    const pPostal = n(party.postalCode);
    const pCity = n(party.city);
    const pCountry = nCountry(party.country);
    return list.some(
      (e) =>
        n(e.name) === pName &&
        n(e.address1) === pAddr &&
        n(e.postalCode) === pPostal &&
        n(e.city) === pCity &&
        nCountry(e.country) === pCountry
    );
  }

  if (!isLoading && !isAuthenticated) {
    router.replace("/login");
    return null;
  }
  if (isSuperAdmin) {
    router.replace("/bookings");
    return null;
  }

  function runBookingValidation(): Record<string, string> {
    return validateBookingCreateForm(
      {
        postalService,
        shipper,
        receiver,
        payer,
        pickUpAddress,
        deliveryPoint,
        payerSource,
        quickBooking,
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
        packages,
      },
      bookingRules,
      t("fieldRequired")
    );
  }

  function buildBody(): CreateBookingBody {
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
        grossWeight || grossVolume || packageQuantity || pickupHandlingInstructions || deliveryHandlingInstructions || generalInstructions || packages.some((p) => p.weight || p.volume || p.packageType || p.description)
          ? {
              grossWeight: trimOrUndefined(grossWeight),
              grossVolume: trimOrUndefined(grossVolume),
              packageQuantity: trimOrUndefined(packageQuantity) || (packages.length > 0 ? String(packages.length) : undefined),
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

  function toPartyPayload(p: CreateBookingParty): CreateBookingParty | undefined {
    const has = p.name || p.address1 || p.postalCode || p.city || p.country || p.email || p.phoneNumber || p.contactPersonName || p.vatNo || p.customerNumber;
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

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    if (couriersReady && courierIds.length === 0) {
      setError(t("noCouriersBannerTitle"));
      return;
    }
    const validationErrors = runBookingValidation();
    if (Object.keys(validationErrors).length > 0) {
      setFieldErrors(validationErrors);
      setError(null);
      setTimeout(() =>
        document.getElementById("create-booking-validation-banner")?.scrollIntoView({
          behavior: "smooth",
          block: "nearest",
        })
      );
      return;
    }
    setFieldErrors({});
    setError(null);
    setSubmitting(true);
    try {
      const created = await bookingCreate(token, buildBody());
      if (saveToAddressBook && addressBook) {
        if ((shipper.name || shipper.address1) && !partyExistsInAddressList(shipper, addressBook.senders)) {
          try {
            await addSender(token, partyToAddressEntry(shipper));
          } catch { /* ignore */ }
        }
        if ((receiver.name || receiver.address1) && !partyExistsInAddressList(receiver, addressBook.receivers)) {
          try {
            await addReceiver(token, partyToAddressEntry(receiver));
          } catch { /* ignore */ }
        }
      }
      router.push(`/bookings/${created.id}?printWaybill=1`);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("createFailed"));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleSaveDraft(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    if (couriersReady && courierIds.length === 0) {
      setError(t("noCouriersBannerTitle"));
      return;
    }
    setFieldErrors({});
    setError(null);
    setSubmitting(true);
    try {
      const created = await draftCreate(token, buildBody());
      if (saveToAddressBook && addressBook) {
        if ((shipper.name || shipper.address1) && !partyExistsInAddressList(shipper, addressBook.senders)) {
          try {
            await addSender(token, partyToAddressEntry(shipper));
          } catch { /* ignore */ }
        }
        if ((receiver.name || receiver.address1) && !partyExistsInAddressList(receiver, addressBook.receivers)) {
          try {
            await addReceiver(token, partyToAddressEntry(receiver));
          } catch { /* ignore */ }
        }
      }
      router.push(`/bookings/draft/${created.id}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("saveDraftFailed"));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/bookings">
          <Button variant="ghost">{t("back")}</Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">{t("createTitle")}</h1>
      </div>
      <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
        )}
        {couriersReady && courierIds.length === 0 && (
          <div
            className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-4 space-y-2"
            role="status"
          >
            <p className="text-sm font-medium">{t("noCouriersBannerTitle")}</p>
            <p className="text-sm text-muted-foreground">
              {isAdmin ? t("noCouriersBannerAdmin") : t("noCouriersBannerUser")}
            </p>
            <Link href="/company/courier-contracts" className="text-sm font-medium text-primary underline">
              {t("goToCourierContracts")}
            </Link>
          </div>
        )}

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="quickBooking"
                checked={quickBooking}
                onChange={(e) => setQuickBooking(e.target.checked)}
                className="h-4 w-4 rounded border-input"
              />
              <Label htmlFor="quickBooking" className="font-medium cursor-pointer">{t("quickBooking")}</Label>
            </div>
            <p className="text-muted-foreground text-sm mt-1">{t("quickBookingDescription")}</p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6 grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="postalService">{tSections("selectCourier")}</Label>
              <select
                id="postalService"
                className="flex h-9 w-full max-w-xs rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                value={postalService}
                onChange={(e) => setPostalService(e.target.value)}
              >
                <option value="">— {tSections("selectCourier")} —</option>
                {courierIds.map((id) => (
                  <option key={id} value={id}>
                    {id}
                  </option>
                ))}
              </select>
              {fieldErrors["courier.postalService"] ? (
                <p className="text-xs text-destructive" role="alert">
                  {fieldErrors["courier.postalService"]}
                </p>
              ) : null}
            </div>
            <div className="space-y-2">
              <Label htmlFor="companyName">{tFields("companyName")}</Label>
              <Input id="companyName" value={referenceNumber} readOnly className="bg-muted" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="businessId">{tFields("businessId")}</Label>
              <Input id="businessId" value={companyId} readOnly className="bg-muted" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("shipper")}</CardTitle>
            <CardDescription>{tSections("shipperDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {addressBook && addressBook.senders.length > 0 && (
              <div className="flex items-center gap-2">
                <Label className="text-sm text-muted-foreground shrink-0">{tFields("fromAddressBook")}</Label>
                <select
                  className="flex h-9 w-full max-w-xs rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                  value=""
                  onChange={(e) => {
                    const id = e.target.value;
                    const s = addressBook.senders.find((x) => x.id === id || (x.name && id === "name:" + x.name));
                    if (s) setShipper(addressToParty(s));
                  }}
                >
                  <option value="">{tFields("selectSender")}</option>
                  {addressBook.senders.map((s) => (
                    <option key={s.id ?? s.name} value={s.id ?? "name:" + (s.name ?? "")}>
                      {s.name || s.city || s.address1 || "—"}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <PartyFields
              title=""
              state={shipper}
              setState={setShipper}
              quickBooking={quickBooking}
              fieldKeyPrefix="shipper"
              fieldErrors={fieldErrors}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("receiver")}</CardTitle>
            <CardDescription>{tSections("receiverDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {addressBook && addressBook.receivers.length > 0 && (
              <div className="flex items-center gap-2">
                <Label className="text-sm text-muted-foreground shrink-0">{tFields("fromAddressBook")}</Label>
                <select
                  className="flex h-9 w-full max-w-xs rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm"
                  value=""
                  onChange={(e) => {
                    const id = e.target.value;
                    const r = addressBook.receivers.find((x) => x.id === id || (x.name && id === "name:" + x.name));
                    if (r) setReceiver(addressToParty(r));
                  }}
                >
                  <option value="">{tFields("selectReceiver")}</option>
                  {addressBook.receivers.map((r) => (
                    <option key={r.id ?? r.name} value={r.id ?? "name:" + (r.name ?? "")}>
                      {r.name || r.city || r.address1 || "—"}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <PartyFields
              title=""
              state={receiver}
              setState={setReceiver}
              quickBooking={quickBooking}
              fieldKeyPrefix="receiver"
              fieldErrors={fieldErrors}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("payer")}</CardTitle>
            <CardDescription>{tSections("payerDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>{tFields("payerSameAs")}</Label>
              <div className="flex flex-wrap gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="payerSource"
                    checked={payerSource === "sender"}
                    onChange={() => setPayerSource("sender")}
                    className="h-4 w-4"
                  />
                  <span>{tFields("sender")}</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="payerSource"
                    checked={payerSource === "receiver"}
                    onChange={() => setPayerSource("receiver")}
                    className="h-4 w-4"
                  />
                  <span>{tFields("receiver")}</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="payerSource"
                    checked={payerSource === "other"}
                    onChange={() => setPayerSource("other")}
                    className="h-4 w-4"
                  />
                  <span>{tFields("other")}</span>
                </label>
              </div>
            </div>
            {payerSource === "other" ? (
              <PartyFields
                title=""
                state={payer}
                setState={setPayer}
                quickBooking={quickBooking}
                fieldKeyPrefix="payer"
                fieldErrors={fieldErrors}
              />
            ) : (
              <p className="text-sm text-muted-foreground">
                {tFields("payerUseDetails", { role: payerSource === "sender" ? tFields("shipper") : tFields("receiver") })}
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("pickupAddress")}</CardTitle>
            <CardDescription>{tSections("pickupAddressDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <PartyFields
              title=""
              state={pickUpAddress}
              setState={setPickUpAddress}
              quickBooking={quickBooking}
              fieldKeyPrefix="pickupAddress"
              fieldErrors={fieldErrors}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("deliveryPoint")}</CardTitle>
            <CardDescription>{tSections("deliveryPointDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <PartyFields
              title=""
              state={deliveryPoint}
              setState={setDeliveryPoint}
              quickBooking={quickBooking}
              fieldKeyPrefix="deliveryPoint"
              fieldErrors={fieldErrors}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("shipment")}</CardTitle>
            <CardDescription>{tSections("shipmentDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>{tFields("service")}</Label>
              <Input value={service} onChange={(e) => setService(e.target.value)} placeholder={t("optional")} />
              {fieldErrors["shipment.service"] ? (
                <p className="text-xs text-destructive" role="alert">
                  {fieldErrors["shipment.service"]}
                </p>
              ) : null}
            </div>
            {!quickBooking && (
              <>
                <div className="space-y-2">
                  <Label>{tFields("senderReference")}</Label>
                  <Input value={senderReference} onChange={(e) => setSenderReference(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shipment.senderReference"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shipment.senderReference"]}
                    </p>
                  ) : null}
                </div>
                <div className="space-y-2">
                  <Label>{tFields("receiverReference")}</Label>
                  <Input value={receiverReference} onChange={(e) => setReceiverReference(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shipment.receiverReference"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shipment.receiverReference"]}
                    </p>
                  ) : null}
                </div>
                <div className="space-y-2">
                  <Label>{tFields("freightPayer")}</Label>
                  <Input value={freightPayer} onChange={(e) => setFreightPayer(e.target.value)} placeholder={tFields("freightPayerPlaceholder")} />
                  {fieldErrors["shipment.freightPayer"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shipment.freightPayer"]}
                    </p>
                  ) : null}
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label>{tFields("handlingInstructions")}</Label>
                  <Input value={handlingInstructions} onChange={(e) => setHandlingInstructions(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shipment.handlingInstructions"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shipment.handlingInstructions"]}
                    </p>
                  ) : null}
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("shippingInfo")}</CardTitle>
            <CardDescription>{tSections("shippingInfoDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-2">
                <Label>{tFields("grossWeight")}</Label>
                <Input value={grossWeight} onChange={(e) => setGrossWeight(e.target.value)} placeholder={tFields("grossWeightPlaceholder")} />
                {fieldErrors["shippingInfo.grossWeight"] ? (
                  <p className="text-xs text-destructive" role="alert">
                    {fieldErrors["shippingInfo.grossWeight"]}
                  </p>
                ) : null}
              </div>
              {!quickBooking && (
                <div className="space-y-2">
                  <Label>{tFields("grossVolume")}</Label>
                  <Input value={grossVolume} onChange={(e) => setGrossVolume(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shippingInfo.grossVolume"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shippingInfo.grossVolume"]}
                    </p>
                  ) : null}
                </div>
              )}
              <div className="space-y-2">
                <Label>{tFields("packageQuantity")}</Label>
                <Input value={packageQuantity} onChange={(e) => setPackageQuantity(e.target.value)} placeholder={tFields("packageQuantityPlaceholder")} />
                {fieldErrors["shippingInfo.packageQuantity"] ? (
                  <p className="text-xs text-destructive" role="alert">
                    {fieldErrors["shippingInfo.packageQuantity"]}
                  </p>
                ) : null}
              </div>
            </div>
            {!quickBooking && (
              <>
                <div className="space-y-2">
                  <Label>{tFields("pickupHandlingInstructions")}</Label>
                  <Input value={pickupHandlingInstructions} onChange={(e) => setPickupHandlingInstructions(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shippingInfo.pickupHandlingInstructions"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shippingInfo.pickupHandlingInstructions"]}
                    </p>
                  ) : null}
                </div>
                <div className="space-y-2">
                  <Label>{tFields("deliveryHandlingInstructions")}</Label>
                  <Input value={deliveryHandlingInstructions} onChange={(e) => setDeliveryHandlingInstructions(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shippingInfo.deliveryHandlingInstructions"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shippingInfo.deliveryHandlingInstructions"]}
                    </p>
                  ) : null}
                </div>
                <div className="space-y-2">
                  <Label>{tFields("generalInstructions")}</Label>
                  <Input value={generalInstructions} onChange={(e) => setGeneralInstructions(e.target.value)} placeholder={t("optional")} />
                  {fieldErrors["shippingInfo.generalInstructions"] ? (
                    <p className="text-xs text-destructive" role="alert">
                      {fieldErrors["shippingInfo.generalInstructions"]}
                    </p>
                  ) : null}
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="noSignature"
                    checked={deliveryWithoutSignature}
                    onChange={(e) => setDeliveryWithoutSignature(e.target.checked)}
                    className="h-4 w-4 rounded border-input"
                  />
                  <Label htmlFor="noSignature">{tFields("deliveryWithoutSignature")}</Label>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{tSections("packages")}</CardTitle>
            <CardDescription>{tSections("packagesDescription")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {packages.map((pkg, index) => (
              <div key={index} className="rounded-lg border p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">{tFields("package", { number: index + 1 })}</span>
                  {packages.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => setPackages((prev) => prev.filter((_, i) => i !== index))}
                    >
                      {tFields("remove")}
                    </Button>
                  )}
                </div>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                  <div className="space-y-1">
                    <Label>{tFields("weight")}</Label>
                    <Input
                      value={pkg.weight ?? ""}
                      onChange={(e) =>
                        setPackages((prev) =>
                          prev.map((p, i) => (i === index ? { ...p, weight: e.target.value } : p))
                        )
                      }
                      placeholder={tFields("weightPlaceholder")}
                    />
                    {fieldErrors[`package.${index}.weight`] ? (
                      <p className="text-xs text-destructive" role="alert">
                        {fieldErrors[`package.${index}.weight`]}
                      </p>
                    ) : null}
                  </div>
                  {!quickBooking && (
                    <div className="space-y-1">
                      <Label>{tFields("volume")}</Label>
                      <Input
                        value={pkg.volume ?? ""}
                        onChange={(e) =>
                          setPackages((prev) =>
                            prev.map((p, i) => (i === index ? { ...p, volume: e.target.value } : p))
                          )
                        }
                        placeholder={t("optional")}
                      />
                      {fieldErrors[`package.${index}.volume`] ? (
                        <p className="text-xs text-destructive" role="alert">
                          {fieldErrors[`package.${index}.volume`]}
                        </p>
                      ) : null}
                    </div>
                  )}
                  <div className="space-y-1">
                    <Label>{tFields("packageType")}</Label>
                    <Input
                      value={pkg.packageType ?? ""}
                      onChange={(e) =>
                        setPackages((prev) =>
                          prev.map((p, i) => (i === index ? { ...p, packageType: e.target.value } : p))
                        )
                      }
                      placeholder={tFields("packageTypePlaceholder")}
                    />
                    {fieldErrors[`package.${index}.packageType`] ? (
                      <p className="text-xs text-destructive" role="alert">
                        {fieldErrors[`package.${index}.packageType`]}
                      </p>
                    ) : null}
                  </div>
                  {!quickBooking && (
                    <>
                      <div className="space-y-1 sm:col-span-2">
                        <Label>{tFields("description")}</Label>
                        <Input
                          value={pkg.description ?? ""}
                          onChange={(e) =>
                            setPackages((prev) =>
                              prev.map((p, i) => (i === index ? { ...p, description: e.target.value } : p))
                            )
                          }
                          placeholder={t("optional")}
                        />
                        {fieldErrors[`package.${index}.description`] ? (
                          <p className="text-xs text-destructive" role="alert">
                            {fieldErrors[`package.${index}.description`]}
                          </p>
                        ) : null}
                      </div>
                      <div className="space-y-1">
                        <Label>{tFields("length")}</Label>
                        <Input
                          value={pkg.length ?? ""}
                          onChange={(e) =>
                            setPackages((prev) =>
                              prev.map((p, i) => (i === index ? { ...p, length: e.target.value } : p))
                            )
                          }
                          placeholder={t("optional")}
                        />
                        {fieldErrors[`package.${index}.length`] ? (
                          <p className="text-xs text-destructive" role="alert">
                            {fieldErrors[`package.${index}.length`]}
                          </p>
                        ) : null}
                      </div>
                      <div className="space-y-1">
                        <Label>{tFields("width")}</Label>
                        <Input
                          value={pkg.width ?? ""}
                          onChange={(e) =>
                            setPackages((prev) =>
                              prev.map((p, i) => (i === index ? { ...p, width: e.target.value } : p))
                            )
                          }
                          placeholder={t("optional")}
                        />
                        {fieldErrors[`package.${index}.width`] ? (
                          <p className="text-xs text-destructive" role="alert">
                            {fieldErrors[`package.${index}.width`]}
                          </p>
                        ) : null}
                      </div>
                      <div className="space-y-1">
                        <Label>{tFields("height")}</Label>
                        <Input
                          value={pkg.height ?? ""}
                          onChange={(e) =>
                            setPackages((prev) =>
                              prev.map((p, i) => (i === index ? { ...p, height: e.target.value } : p))
                            )
                          }
                          placeholder={t("optional")}
                        />
                        {fieldErrors[`package.${index}.height`] ? (
                          <p className="text-xs text-destructive" role="alert">
                            {fieldErrors[`package.${index}.height`]}
                          </p>
                        ) : null}
                      </div>
                    </>
                  )}
                </div>
              </div>
            ))}
            <Button
              type="button"
              variant="outline"
              onClick={() => setPackages((prev) => [...prev, defaultPackage()])}
            >
              {tFields("addPackage")}
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-3 border-t pt-4">
          <label className="flex items-center gap-2 text-sm cursor-pointer">
            <input
              type="checkbox"
              checked={saveToAddressBook}
              onChange={(e) => setSaveToAddressBook(e.target.checked)}
              className="h-4 w-4 rounded border"
            />
            <span>{t("saveToAddressBook")}</span>
          </label>
          <div className="flex flex-wrap items-center gap-3">
            {Object.keys(fieldErrors).length > 0 ? (
              <p className="text-sm text-destructive shrink-0 max-w-md" role="alert" id="create-booking-validation-banner">
                {t("validationActionBanner")}
              </p>
            ) : null}
            <Button
              type="submit"
              disabled={submitting || (couriersReady && courierIds.length === 0)}
              aria-describedby={Object.keys(fieldErrors).length > 0 ? "create-booking-validation-banner" : undefined}
            >
              {submitting ? t("creating") : t("createTitle")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={submitting || (couriersReady && courierIds.length === 0)}
              onClick={handleSaveDraft}
            >
              {submitting ? t("saving") : t("saveAsDraft")}
            </Button>
            <Link href="/bookings">
              <Button type="button" variant="outline">
                {t("cancel")}
              </Button>
            </Link>
          </div>
        </div>
      </form>
    </div>
  );
}
