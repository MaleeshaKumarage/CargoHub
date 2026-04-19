"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { riderAcceptBooking, riderGetBooking, type BookingDetail } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export default function RiderDeliveryDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const { token, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("rider");
  const tFr = useTranslations("bookings.freelanceRider");
  const [booking, setBooking] = useState<BookingDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [accepting, setAccepting] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) router.replace("/login");
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (!token || !id) return;
    setLoading(true);
    setError(null);
    riderGetBooking(token, id)
      .then(setBooking)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed"))
      .finally(() => setLoading(false));
  }, [token, id]);

  async function onAccept() {
    if (!token || !id) return;
    setAccepting(true);
    setError(null);
    try {
      await riderAcceptBooking(token, id);
      const updated = await riderGetBooking(token, id);
      setBooking(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Accept failed");
    } finally {
      setAccepting(false);
    }
  }

  if (!isAuthenticated || isLoading) return null;

  const pending =
    booking &&
    !booking.freelanceRiderAcceptedAtUtc &&
    !booking.freelanceRiderAssignmentLapsed &&
    booking.freelanceRiderAssignmentDeadlineUtc &&
    new Date(booking.freelanceRiderAssignmentDeadlineUtc) > new Date();

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/rider/deliveries">
          <Button variant="ghost" type="button">
            Back
          </Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">{t("deliveriesTitle")}</h1>
      </div>
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}
      {loading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : !booking ? (
        <p className="text-muted-foreground">Not found.</p>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{booking.header?.referenceNumber ?? booking.id.slice(0, 8)}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            {booking.receiver ? (
              <div>
                <p className="font-medium">{booking.receiver.name}</p>
                <p className="text-muted-foreground">
                  {booking.receiver.address1}, {booking.receiver.postalCode} {booking.receiver.city}
                </p>
              </div>
            ) : null}
            {booking.freelanceRiderAssignmentLapsed ? (
              <p>{tFr("lapsed")}</p>
            ) : booking.freelanceRiderAcceptedAtUtc ? (
              <p>{tFr("accepted")}</p>
            ) : pending ? (
              <div className="space-y-2">
                <p>{tFr("pending")}</p>
                {booking.freelanceRiderAssignmentDeadlineUtc ? (
                  <p className="text-muted-foreground text-xs">
                    {tFr("deadline", {
                      time: new Date(booking.freelanceRiderAssignmentDeadlineUtc).toLocaleString(),
                    })}
                  </p>
                ) : null}
                <Button type="button" size="sm" disabled={accepting} onClick={onAccept}>
                  {accepting ? t("accepting") : t("accept")}
                </Button>
              </div>
            ) : (
              <p className="text-muted-foreground">—</p>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
