"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useParams, useSearchParams } from "next/navigation";
import { useEffect, useState, useRef } from "react";
import { bookingGet, getWaybillPdfBlobUrl, type BookingDetail, type BookingDetailParty } from "@/lib/api";
import { isRiderOnlyPortal } from "@/lib/rider-role";
import { useTranslations } from "next-intl";
import { BookingMilestoneBar } from "@/components/BookingMilestoneBar";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Package,
  User,
  MapPin,
  Truck,
  FileText,
  CreditCard,
  Calendar,
  Hash,
  Printer,
} from "lucide-react";

function PartyBlock({
  title,
  party,
  icon: Icon,
}: {
  title: string;
  party: BookingDetailParty | null | undefined;
  icon: React.ElementType;
}) {
  if (!party) return null;
  return (
    <Card className="h-full">
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-base">
          <Icon className="h-4 w-4 text-muted-foreground" />
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 text-sm">
        <p className="font-medium">{party.name || "—"}</p>
        <p className="text-muted-foreground">{party.address1}</p>
        {party.address2 && <p className="text-muted-foreground">{party.address2}</p>}
        <p className="text-muted-foreground">
          {party.postalCode} {party.city}, {party.country}
        </p>
        {(party.email || party.phoneNumber || party.phoneNumberMobile) && (
          <div className="mt-2 pt-2 border-t border-border/50 space-y-0.5">
            {party.email && <p>{party.email}</p>}
            {(party.phoneNumber || party.phoneNumberMobile) && (
              <p>{party.phoneNumber || party.phoneNumberMobile}</p>
            )}
          </div>
        )}
        {(party.contactPersonName || party.vatNo || party.customerNumber) && (
          <div className="mt-1 space-y-0.5 text-muted-foreground">
            {party.contactPersonName && <p>Contact: {party.contactPersonName}</p>}
            {party.vatNo && <p>VAT: {party.vatNo}</p>}
            {party.customerNumber && <p>Customer #: {party.customerNumber}</p>}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default function BookingDetailPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const id = params?.id as string;
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const tFr = useTranslations("bookings.freelanceRider");
  const [booking, setBooking] = useState<BookingDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [waybillLoading, setWaybillLoading] = useState(false);
  const autoPrintDone = useRef(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token || !id) return;
    setLoading(true);
    setError(null);
    bookingGet(token, id)
      .then(setBooking)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, id, isAuthenticated, isLoading, router]);

  // Auto-open waybill after completion when redirected with ?printWaybill=1
  useEffect(() => {
    if (autoPrintDone.current || !token || !id || !booking || loading) return;
    if (booking.isDraft) return;
    if (searchParams?.get("printWaybill") !== "1") return;
    autoPrintDone.current = true;
    getWaybillPdfBlobUrl(token, id)
      .then((url) => {
        const w = window.open(url, "_blank", "noopener");
        if (w) setTimeout(() => URL.revokeObjectURL(url), 60000);
        // Refetch so milestone bar shows Waybill status
        return bookingGet(token, id);
      })
      .then((updated) => updated != null && setBooking(updated))
      .catch(() => {});
    // Remove query param from URL without reload
    if (typeof window !== "undefined" && window.history.replaceState)
      window.history.replaceState({}, "", window.location.pathname);
  }, [booking, loading, token, id, searchParams]);

  async function handlePrintWaybill() {
    if (!token || !id) return;
    setWaybillLoading(true);
    setError(null);
    try {
      const url = await getWaybillPdfBlobUrl(token, id);
      window.open(url, "_blank", "noopener");
      setTimeout(() => URL.revokeObjectURL(url), 60000);
      // Refetch booking so milestone bar shows updated Waybill status
      const updated = await bookingGet(token, id);
      setBooking(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load waybill");
    } finally {
      setWaybillLoading(false);
    }
  }

  if (!isAuthenticated || isLoading) return null;
  if (user?.roles && isRiderOnlyPortal(user.roles)) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-muted-foreground">Redirecting…</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-4">
        <Link href="/bookings">
          <Button variant="ghost">Back</Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">
          Booking {id?.slice(0, 8)}
        </h1>
        {booking && !booking.isDraft && (
          <Button onClick={handlePrintWaybill} disabled={waybillLoading} variant="outline" className="gap-2">
            <Printer className="h-4 w-4" />
            {waybillLoading ? "Loading…" : "Print waybill"}
          </Button>
        )}
      </div>
      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}
      {loading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : booking ? (
        <div className="space-y-6">
          {/* Milestone bar (uses traced statusHistory when available) */}
          <BookingMilestoneBar item={booking} className="max-w-full" />

          {/* Summary strip */}
          <Card className="bg-muted/30">
            <CardContent className="py-4">
              <div className="flex flex-wrap items-center gap-6 text-sm">
                <span className="flex items-center gap-1.5">
                  <Hash className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Ref:</span> {booking.header?.referenceNumber ?? "—"}
                </span>
                <span className="flex items-center gap-1.5">
                  <Truck className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Service:</span> {booking.header?.postalService ?? "—"}
                </span>
                {booking.shipmentNumber && (
                  <span>
                    <span className="font-medium">Shipment #:</span> {booking.shipmentNumber}
                  </span>
                )}
                {booking.waybillNumber && (
                  <span>
                    <span className="font-medium">Waybill:</span> {booking.waybillNumber}
                  </span>
                )}
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  {new Date(booking.createdAtUtc).toLocaleDateString()}
                </span>
                <span className="rounded-full bg-background px-2 py-0.5 text-xs font-medium border">
                  {booking.enabled ? "Enabled" : "Disabled"}
                </span>
              </div>
            </CardContent>
          </Card>

          {booking.freelanceRiderId ? (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-base">{tFr("detailHeading")}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                {booking.freelanceRiderAssignmentLapsed ? (
                  <p>{tFr("lapsed")}</p>
                ) : booking.freelanceRiderAcceptedAtUtc ? (
                  <p>
                    {tFr("accepted")}
                    {booking.freelanceRiderAcceptedAtUtc
                      ? ` (${new Date(booking.freelanceRiderAcceptedAtUtc).toLocaleString()})`
                      : ""}
                  </p>
                ) : booking.freelanceRiderAssignmentDeadlineUtc ? (
                  <div className="space-y-1">
                    <p>{tFr("pending")}</p>
                    <p className="text-muted-foreground">
                      {tFr("deadline", {
                        time: new Date(booking.freelanceRiderAssignmentDeadlineUtc).toLocaleString(),
                      })}
                    </p>
                  </div>
                ) : (
                  <p>{tFr("pending")}</p>
                )}
              </CardContent>
            </Card>
          ) : null}

          {/* Sender | Receiver side by side */}
          <div className="grid gap-4 md:grid-cols-2">
            <PartyBlock
              title="Sender (shipper)"
              party={booking.shipper ?? undefined}
              icon={User}
            />
            <PartyBlock
              title="Receiver"
              party={booking.receiver ?? undefined}
              icon={MapPin}
            />
          </div>

          {/* Payer */}
          {booking.payer && (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-base">
                  <CreditCard className="h-4 w-4 text-muted-foreground" />
                  Payer
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 text-sm">
                  <p className="font-medium">{booking.payer.name || "—"}</p>
                  <p className="text-muted-foreground">{booking.payer.address1}, {booking.payer.postalCode} {booking.payer.city}</p>
                  {(booking.payer.email || booking.payer.phoneNumber) && (
                    <p className="text-muted-foreground">{booking.payer.email || booking.payer.phoneNumber}</p>
                  )}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Pickup | Delivery side by side */}
          {(booking.pickUpAddress || booking.deliveryPoint) && (
            <div className="grid gap-4 md:grid-cols-2">
              <PartyBlock
                title="Pickup address"
                party={booking.pickUpAddress ?? undefined}
                icon={MapPin}
              />
              <PartyBlock
                title="Delivery point"
                party={booking.deliveryPoint ?? undefined}
                icon={MapPin}
              />
            </div>
          )}

          {/* Shipment & Shipping info side by side */}
          <div className="grid gap-4 md:grid-cols-2">
            {booking.shipment && (booking.shipment.service || booking.shipment.senderReference || booking.shipment.receiverReference || booking.shipment.freightPayer || booking.shipment.handlingInstructions) && (
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="flex items-center gap-2 text-base">
                    <Truck className="h-4 w-4 text-muted-foreground" />
                    Shipment
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-1.5 text-sm">
                  {booking.shipment.service && <p><span className="text-muted-foreground">Service:</span> {booking.shipment.service}</p>}
                  {booking.shipment.senderReference && <p><span className="text-muted-foreground">Sender ref:</span> {booking.shipment.senderReference}</p>}
                  {booking.shipment.receiverReference && <p><span className="text-muted-foreground">Receiver ref:</span> {booking.shipment.receiverReference}</p>}
                  {booking.shipment.freightPayer && <p><span className="text-muted-foreground">Freight payer:</span> {booking.shipment.freightPayer}</p>}
                  {booking.shipment.handlingInstructions && <p><span className="text-muted-foreground">Handling:</span> {booking.shipment.handlingInstructions}</p>}
                </CardContent>
              </Card>
            )}
            {booking.shippingInfo && (booking.shippingInfo.grossWeight || booking.shippingInfo.grossVolume || booking.shippingInfo.packageQuantity || booking.shippingInfo.generalInstructions) && (
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="flex items-center gap-2 text-base">
                    <FileText className="h-4 w-4 text-muted-foreground" />
                    Shipping info
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-1.5 text-sm">
                  {booking.shippingInfo.grossWeight != null && <p><span className="text-muted-foreground">Gross weight:</span> {booking.shippingInfo.grossWeight}</p>}
                  {booking.shippingInfo.grossVolume != null && <p><span className="text-muted-foreground">Gross volume:</span> {booking.shippingInfo.grossVolume}</p>}
                  {booking.shippingInfo.packageQuantity != null && <p><span className="text-muted-foreground">Package qty:</span> {booking.shippingInfo.packageQuantity}</p>}
                  {booking.shippingInfo.pickupHandlingInstructions && <p><span className="text-muted-foreground">Pickup:</span> {booking.shippingInfo.pickupHandlingInstructions}</p>}
                  {booking.shippingInfo.deliveryHandlingInstructions && <p><span className="text-muted-foreground">Delivery:</span> {booking.shippingInfo.deliveryHandlingInstructions}</p>}
                  {booking.shippingInfo.generalInstructions && <p><span className="text-muted-foreground">General:</span> {booking.shippingInfo.generalInstructions}</p>}
                  {booking.shippingInfo.deliveryWithoutSignature != null && <p>Delivery without signature: {booking.shippingInfo.deliveryWithoutSignature ? "Yes" : "No"}</p>}
                </CardContent>
              </Card>
            )}
          </div>

          {/* Packages list */}
          {booking.packages && booking.packages.length > 0 && (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Package className="h-4 w-4 text-muted-foreground" />
                  Packages ({booking.packages.length})
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {booking.packages.map((pkg, i) => (
                    <li
                      key={pkg.id}
                      className="flex items-start gap-3 rounded-lg border bg-muted/20 p-3"
                    >
                      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10">
                        <Package className="h-4 w-4 text-primary" />
                      </div>
                      <div className="min-w-0 flex-1 text-sm">
                        <p className="font-medium">Package {i + 1}</p>
                        <div className="mt-1 flex flex-wrap gap-x-4 gap-y-0.5 text-muted-foreground">
                          {pkg.weight && <span>Weight: {pkg.weight}</span>}
                          {pkg.volume && <span>Volume: {pkg.volume}</span>}
                          {pkg.packageType && <span>Type: {pkg.packageType}</span>}
                          {(pkg.length || pkg.width || pkg.height) && (
                            <span>Dimensions: {pkg.length ?? "—"} × {pkg.width ?? "—"} × {pkg.height ?? "—"}</span>
                          )}
                        </div>
                        {pkg.description && <p className="mt-1">{pkg.description}</p>}
                      </div>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}
        </div>
      ) : null}
    </div>
  );
}
