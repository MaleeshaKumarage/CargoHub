"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useCallback, useEffect, useState } from "react";
import { riderAcceptBooking, riderListBookings, type BookingDetail } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function RiderDeliveriesPage() {
  const { token, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("rider");
  const tFr = useTranslations("bookings.freelanceRider");
  const [list, setList] = useState<BookingDetail[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(() => {
    if (!token) return;
    setLoading(true);
    setError(null);
    riderListBookings(token)
      .then(setList)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed"))
      .finally(() => setLoading(false));
  }, [token]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    load();
  }, [load]);

  async function onAccept(id: string) {
    if (!token) return;
    setBusyId(id);
    setError(null);
    try {
      await riderAcceptBooking(token, id);
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Accept failed");
    } finally {
      setBusyId(null);
    }
  }

  if (!isAuthenticated || isLoading) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("deliveriesTitle")}</h1>
        <p className="text-muted-foreground mt-1">{t("deliveriesDescription")}</p>
      </div>
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}
      {loading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : list.length === 0 ? (
        <Card>
          <CardContent className="pt-6">
            <p className="text-muted-foreground">{t("noDeliveries")}</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {list.map((b) => {
            const pending =
              !b.freelanceRiderAcceptedAtUtc &&
              !b.freelanceRiderAssignmentLapsed &&
              b.freelanceRiderAssignmentDeadlineUtc &&
              new Date(b.freelanceRiderAssignmentDeadlineUtc) > new Date();
            return (
              <Card key={b.id}>
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">
                    <Link href={`/rider/deliveries/${b.id}`} className="text-primary hover:underline">
                      {b.header?.referenceNumber || b.id.slice(0, 8)}
                    </Link>
                  </CardTitle>
                  <CardDescription>
                    {b.shipmentNumber ?? ""}
                    {b.waybillNumber ? ` · ${b.waybillNumber}` : ""}
                  </CardDescription>
                </CardHeader>
                <CardContent className="flex flex-wrap items-center gap-3">
                  {pending ? (
                    <>
                      <Button type="button" size="sm" disabled={busyId === b.id} onClick={() => onAccept(b.id)}>
                        {busyId === b.id ? t("accepting") : t("accept")}
                      </Button>
                      {b.freelanceRiderAssignmentDeadlineUtc ? (
                        <span className="text-xs text-muted-foreground">
                          {new Date(b.freelanceRiderAssignmentDeadlineUtc).toLocaleString()}
                        </span>
                      ) : null}
                    </>
                  ) : (
                    <span className="text-sm text-muted-foreground">
                      {b.freelanceRiderAcceptedAtUtc ? tFr("accepted") : "—"}
                    </span>
                  )}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
