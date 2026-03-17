"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { draftGet, draftUpdate, draftConfirm, type BookingDetail, type UpdateDraftBody } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function DraftDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const isSuperAdmin = Array.isArray(user?.roles) && user.roles.includes("SuperAdmin");
  const [draft, setDraft] = useState<BookingDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [referenceNumber, setReferenceNumber] = useState("");
  const [receiverName, setReceiverName] = useState("");
  const [receiverAddress1, setReceiverAddress1] = useState("");
  const [receiverPostalCode, setReceiverPostalCode] = useState("");
  const [receiverCity, setReceiverCity] = useState("");
  const [receiverCountry, setReceiverCountry] = useState("FI");
  const [receiverEmail, setReceiverEmail] = useState("");
  const [receiverPhone, setReceiverPhone] = useState("");
  const [saving, setSaving] = useState(false);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token || !id) return;
    setLoading(true);
    setError(null);
    draftGet(token, id)
      .then((d) => {
        setDraft(d);
        setReferenceNumber(d.header?.referenceNumber ?? "");
        setReceiverName(d.receiver?.name ?? "");
        setReceiverAddress1(d.receiver?.address1 ?? "");
        setReceiverPostalCode(d.receiver?.postalCode ?? "");
        setReceiverCity(d.receiver?.city ?? "");
        setReceiverCountry(d.receiver?.country ?? "FI");
        setReceiverEmail(d.receiver?.email ?? "");
        setReceiverPhone(d.receiver?.phoneNumber ?? "");
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, id, isAuthenticated, isLoading, router]);

  async function handleSave() {
    if (!token || !id) return;
    setError(null);
    setSaving(true);
    const body: UpdateDraftBody = {
      referenceNumber: referenceNumber.trim() || undefined,
      receiverName: receiverName.trim() || undefined,
      receiverAddress1: receiverAddress1.trim() || undefined,
      receiverPostalCode: receiverPostalCode.trim() || undefined,
      receiverCity: receiverCity.trim() || undefined,
      receiverCountry: receiverCountry.trim() || undefined,
      receiverEmail: receiverEmail.trim() || undefined,
      receiverPhone: receiverPhone.trim() || undefined,
    };
    try {
      const updated = await draftUpdate(token, id, body);
      setDraft(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Update failed");
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirm() {
    if (!token || !id) return;
    setError(null);
    setConfirming(true);
    try {
      const completed = await draftConfirm(token, id);
      router.push(`/bookings/${completed.id}?printWaybill=1`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Confirm failed");
    } finally {
      setConfirming(false);
    }
  }

  if (!isAuthenticated || isLoading) return null;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/bookings">
          <Button variant="ghost">Back</Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">
          Draft {id?.slice(0, 8)}
        </h1>
      </div>
      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}
      {loading ? (
        <p className="text-muted-foreground">Loading…</p>
      ) : draft ? (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{isSuperAdmin ? "View draft" : "Edit draft"}</CardTitle>
              <CardDescription>
                {isSuperAdmin
                  ? "View-only. You cannot edit or confirm this draft."
                  : "Fill in the rest and save. When ready, confirm to complete the booking."}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="reference">Reference number</Label>
                  <Input
                    id="reference"
                    value={referenceNumber}
                    onChange={(e) => setReferenceNumber(e.target.value)}
                    placeholder="Optional"
                    readOnly={isSuperAdmin}
                    className={isSuperAdmin ? "bg-muted" : undefined}
                  />
                </div>
              </div>
              <div className="border-t pt-4 space-y-4">
                <h3 className="font-medium">Receiver</h3>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="receiverName">Name</Label>
                    <Input
                      id="receiverName"
                      value={receiverName}
                      onChange={(e) => setReceiverName(e.target.value)}
                      placeholder="Receiver name"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverAddress1">Address</Label>
                    <Input
                      id="receiverAddress1"
                      value={receiverAddress1}
                      onChange={(e) => setReceiverAddress1(e.target.value)}
                      placeholder="Street address"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverPostalCode">Postal code</Label>
                    <Input
                      id="receiverPostalCode"
                      value={receiverPostalCode}
                      onChange={(e) => setReceiverPostalCode(e.target.value)}
                      placeholder="00100"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverCity">City</Label>
                    <Input
                      id="receiverCity"
                      value={receiverCity}
                      onChange={(e) => setReceiverCity(e.target.value)}
                      placeholder="Helsinki"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverCountry">Country</Label>
                    <Input
                      id="receiverCountry"
                      value={receiverCountry}
                      onChange={(e) => setReceiverCountry(e.target.value)}
                      placeholder="FI"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverEmail">Email</Label>
                    <Input
                      id="receiverEmail"
                      type="email"
                      value={receiverEmail}
                      onChange={(e) => setReceiverEmail(e.target.value)}
                      placeholder="Optional"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverPhone">Phone</Label>
                    <Input
                      id="receiverPhone"
                      value={receiverPhone}
                      onChange={(e) => setReceiverPhone(e.target.value)}
                      placeholder="Optional"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                  </div>
                </div>
              </div>
              {!isSuperAdmin && (
              <div className="flex gap-2 pt-2">
                <Button type="button" onClick={handleSave} disabled={saving}>
                  {saving ? "Saving…" : "Save draft"}
                </Button>
                <Button type="button" onClick={handleConfirm} disabled={confirming}>
                  {confirming ? "Confirming…" : "Confirm & complete booking"}
                </Button>
                <Link href="/bookings">
                  <Button type="button" variant="outline">Cancel</Button>
                </Link>
              </div>
              )}
              {isSuperAdmin && (
                <div className="pt-2">
                  <Link href="/bookings">
                    <Button variant="outline">Back to bookings</Button>
                  </Link>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
