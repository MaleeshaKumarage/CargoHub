"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { bookingList, draftList, type BookingListItem } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BookingMilestoneBar } from "@/components/BookingMilestoneBar";

type Tab = "completed" | "drafts";

export default function BookingsPage() {
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("bookings");
  const [tab, setTab] = useState<Tab>("completed");
  const [list, setList] = useState<BookingListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;
    setLoading(true);
    setError(null);
    const load = tab === "drafts" ? draftList(token) : bookingList(token);
    load
      .then(setList)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, tab, isAuthenticated, isLoading, router]);

  if (!isAuthenticated || isLoading) return null;

  const isDrafts = tab === "drafts";

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        {!isSuperAdmin && (
          <Link href="/bookings/create">
            <Button>{t("createTitle")}</Button>
          </Link>
        )}
      </div>
      <div className="flex gap-2 border-b">
        <button
          type="button"
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${tab === "completed" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
          onClick={() => setTab("completed")}
        >
          Completed
        </button>
        <button
          type="button"
          className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${tab === "drafts" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
          onClick={() => setTab("drafts")}
        >
          Drafts
        </button>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{isDrafts ? "Drafts" : t("title")}</CardTitle>
          <CardDescription>
            {isDrafts
              ? "Save as draft, then retrieve and fill the rest. Confirm to complete."
              : "Your completed bookings. Click a row to view details."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <p className="text-sm text-destructive mb-4" role="alert">
              {error}
            </p>
          )}
          {loading ? (
            <p className="text-muted-foreground">Loading…</p>
          ) : list.length === 0 ? (
            <>
              <p className="text-sm text-muted-foreground mb-4">
                {isDrafts ? "No drafts. Create a booking and choose “Save as draft”." : t("noBookings")}
              </p>
              {!isSuperAdmin && (
                <Link href="/bookings/create">
                  <Button variant="outline">{isDrafts ? "Create booking" : t("createFirst")}</Button>
                </Link>
              )}
            </>
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left font-medium">Reference / Shipment</th>
                    <th className="p-3 text-left font-medium">Customer</th>
                    <th className="p-3 text-left font-medium">Created</th>
                    <th className="p-3 text-left font-medium min-w-[200px]">Milestone</th>
                    {!isDrafts && <th className="p-3 text-left font-medium">Status</th>}
                    <th className="p-3 text-left font-medium"></th>
                  </tr>
                </thead>
                <tbody>
                  {list.map((b) => (
                    <tr key={b.id} className="border-b last:border-0 align-top">
                      <td className="p-3">{b.shipmentNumber || b.id.slice(0, 8)}</td>
                      <td className="p-3">{b.customerName ?? "—"}</td>
                      <td className="p-3">
                        {new Date(b.createdAtUtc).toLocaleDateString()}
                      </td>
                      <td className="p-3">
                        <BookingMilestoneBar item={b} className="max-w-[200px]" />
                      </td>
                      {!isDrafts && (
                        <td className="p-3">{b.enabled ? "Active" : "Disabled"}</td>
                      )}
                      <td className="p-3">
                        {isDrafts ? (
                          isSuperAdmin ? (
                            <Link href={`/bookings/draft/${b.id}`}>
                              <Button variant="ghost" size="sm">
                                View
                              </Button>
                            </Link>
                          ) : (
                            <Link href={`/bookings/draft/${b.id}`}>
                              <Button variant="ghost" size="sm">
                                Edit / Confirm
                              </Button>
                            </Link>
                          )
                        ) : (
                          <Link href={`/bookings/${b.id}`}>
                            <Button variant="ghost" size="sm">
                              View
                            </Button>
                          </Link>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
