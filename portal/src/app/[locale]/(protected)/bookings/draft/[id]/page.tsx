"use client";

import { useAuth } from "@/context/AuthContext";
import { Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState, useCallback } from "react";
import {
  draftGet,
  draftUpdate,
  draftConfirm,
  getBookingFieldRules,
  SubscriptionBillingConflictError,
  type BookingDetail,
  type BookingDetailParty,
  type UpdateDraftBody,
} from "@/lib/api";
import {
  defaultBookingFieldRules,
  parseBookingFieldRulesFromApi,
  validateBookingCreateForm,
  validationContextFromBookingDetail,
  type BookingFieldRules,
} from "@/lib/booking-field-rules";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TrialBookingLimitBanner } from "@/components/TrialBookingLimitBanner";

const emptyParty = (): BookingDetailParty => ({
  name: "",
  address1: "",
  postalCode: "",
  city: "",
  country: "FI",
});

function mergeReceiverFormIntoDraft(
  base: BookingDetail,
  form: {
    referenceNumber: string;
    receiverName: string;
    receiverAddress1: string;
    receiverPostalCode: string;
    receiverCity: string;
    receiverCountry: string;
    receiverEmail: string;
    receiverPhone: string;
  }
): BookingDetail {
  const prev = base.receiver ?? emptyParty();
  return {
    ...base,
    header: {
      ...base.header,
      referenceNumber: form.referenceNumber.trim() || base.header?.referenceNumber || null,
    },
    receiver: {
      ...prev,
      name: form.receiverName.trim(),
      address1: form.receiverAddress1.trim(),
      postalCode: form.receiverPostalCode.trim(),
      city: form.receiverCity.trim(),
      country: form.receiverCountry.trim() || "FI",
      email: form.receiverEmail.trim() || null,
      phoneNumber: form.receiverPhone.trim() || null,
    },
  };
}

export default function DraftDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const t = useTranslations("bookings");
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
  const [bookingRules, setBookingRules] = useState<BookingFieldRules>(() => defaultBookingFieldRules());
  const [confirmFieldErrors, setConfirmFieldErrors] = useState<Record<string, string>>({});
  const [trialBannerOpen, setTrialBannerOpen] = useState(false);
  const [trialBannerMessage, setTrialBannerMessage] = useState("");

  const clearConfirmFieldErrors = useCallback(() => setConfirmFieldErrors({}), []);

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

  useEffect(() => {
    if (!token || isSuperAdmin) return;
    getBookingFieldRules(token)
      .then((api) => setBookingRules(parseBookingFieldRulesFromApi(api)))
      .catch(() => setBookingRules(defaultBookingFieldRules()));
  }, [token, isSuperAdmin]);

  async function handleSave() {
    if (!token || !id) return;
    setError(null);
    clearConfirmFieldErrors();
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
    if (!token || !id || !draft) return;
    setError(null);
    const merged = mergeReceiverFormIntoDraft(draft, {
      referenceNumber,
      receiverName,
      receiverAddress1,
      receiverPostalCode,
      receiverCity,
      receiverCountry,
      receiverEmail,
      receiverPhone,
    });
    const ctx = validationContextFromBookingDetail(merged, false);
    const validationErrors = validateBookingCreateForm(ctx, bookingRules, t("fieldRequired"));
    if (Object.keys(validationErrors).length > 0) {
      setConfirmFieldErrors(validationErrors);
      setTimeout(() =>
        document.getElementById("draft-confirm-validation-banner")?.scrollIntoView({
          behavior: "smooth",
          block: "nearest",
        })
      );
      return;
    }
    setConfirmFieldErrors({});
    setTrialBannerOpen(false);
    setConfirming(true);
    try {
      const completed = await draftConfirm(token, id);
      router.push(`/bookings/${completed.id}?printWaybill=1`);
    } catch (e) {
      if (e instanceof SubscriptionBillingConflictError && e.isTrialBookingLimitExceeded) {
        setTrialBannerMessage(e.message);
        setTrialBannerOpen(true);
        setError(null);
      } else {
        setError(e instanceof Error ? e.message : "Confirm failed");
      }
    } finally {
      setConfirming(false);
    }
  }

  const receiverErr = (suffix: string) => confirmFieldErrors[`receiver.${suffix}`];

  if (!isAuthenticated || isLoading) return null;

  return (
    <>
      <TrialBookingLimitBanner
        open={trialBannerOpen}
        onDismiss={() => setTrialBannerOpen(false)}
        detailMessage={trialBannerMessage}
      />
      <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/bookings">
          <Button variant="ghost">Back</Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">Draft {id?.slice(0, 8)}</h1>
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
                  : "Fill in the rest and save. When ready, confirm to complete the booking. Required fields are checked only when you confirm."}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="reference">Reference number</Label>
                  <Input
                    id="reference"
                    value={referenceNumber}
                    onChange={(e) => {
                      setReferenceNumber(e.target.value);
                      clearConfirmFieldErrors();
                    }}
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
                      onChange={(e) => {
                        setReceiverName(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="Receiver name"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("name") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("name")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverAddress1">Address</Label>
                    <Input
                      id="receiverAddress1"
                      value={receiverAddress1}
                      onChange={(e) => {
                        setReceiverAddress1(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="Street address"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("address1") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("address1")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverPostalCode">Postal code</Label>
                    <Input
                      id="receiverPostalCode"
                      value={receiverPostalCode}
                      onChange={(e) => {
                        setReceiverPostalCode(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="00100"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("postalCode") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("postalCode")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverCity">City</Label>
                    <Input
                      id="receiverCity"
                      value={receiverCity}
                      onChange={(e) => {
                        setReceiverCity(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="Helsinki"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("city") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("city")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverCountry">Country</Label>
                    <Input
                      id="receiverCountry"
                      value={receiverCountry}
                      onChange={(e) => {
                        setReceiverCountry(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="FI"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("country") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("country")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverEmail">Email</Label>
                    <Input
                      id="receiverEmail"
                      type="email"
                      value={receiverEmail}
                      onChange={(e) => {
                        setReceiverEmail(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="Optional"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("email") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("email")}
                      </p>
                    ) : null}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="receiverPhone">Phone</Label>
                    <Input
                      id="receiverPhone"
                      value={receiverPhone}
                      onChange={(e) => {
                        setReceiverPhone(e.target.value);
                        clearConfirmFieldErrors();
                      }}
                      placeholder="Optional"
                      readOnly={isSuperAdmin}
                      className={isSuperAdmin ? "bg-muted" : undefined}
                    />
                    {receiverErr("phoneNumber") ? (
                      <p className="text-xs text-destructive" role="alert">
                        {receiverErr("phoneNumber")}
                      </p>
                    ) : null}
                  </div>
                </div>
              </div>
              {!isSuperAdmin && (
                <div className="space-y-3 border-t pt-4">
                  {Object.keys(confirmFieldErrors).length > 0 ? (
                    <p
                      className="text-sm text-destructive"
                      role="alert"
                      id="draft-confirm-validation-banner"
                    >
                      {t("validationActionBanner")}
                    </p>
                  ) : null}
                  <div className="flex flex-wrap gap-2">
                    <Button type="button" onClick={handleSave} disabled={saving}>
                      {saving ? "Saving…" : "Save draft"}
                    </Button>
                    <Button
                      type="button"
                      onClick={() => void handleConfirm()}
                      disabled={confirming}
                      aria-describedby={
                        Object.keys(confirmFieldErrors).length > 0
                          ? "draft-confirm-validation-banner"
                          : undefined
                      }
                    >
                      {confirming ? "Confirming…" : "Confirm & complete booking"}
                    </Button>
                    <Link href="/bookings">
                      <Button type="button" variant="outline">
                        Cancel
                      </Button>
                    </Link>
                  </div>
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
    </>
  );
}
