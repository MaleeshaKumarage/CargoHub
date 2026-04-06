"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useTranslations } from "next-intl";
import type { CreateBookingParty } from "@/lib/api";

export function PartyFields({
  title,
  state,
  setState,
  quickBooking,
  fieldKeyPrefix,
  fieldErrors,
  readOnly,
}: {
  title: string;
  state: CreateBookingParty;
  setState: (p: CreateBookingParty) => void;
  quickBooking?: boolean;
  fieldKeyPrefix: string;
  fieldErrors?: Record<string, string>;
  readOnly?: boolean;
}) {
  const f = useTranslations("bookings.fields");
  const tBookings = useTranslations("bookings");
  const optional = tBookings("optional");
  const update = (key: keyof CreateBookingParty, value: string) => setState({ ...state, [key]: value });
  const errs = fieldErrors ?? {};
  const err = (key: string) => errs[`${fieldKeyPrefix}.${key}`];
  const ro = readOnly === true;
  return (
    <div className="space-y-3">
      {title ? <h4 className="font-medium text-sm">{title}</h4> : null}
      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-1">
          <Label>{f("name")}</Label>
          <Input
            value={state.name ?? ""}
            onChange={(e) => update("name", e.target.value)}
            placeholder={f("namePlaceholder")}
            readOnly={ro}
            className={ro ? "bg-muted" : undefined}
          />
          {err("name") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("name")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("address")}</Label>
          <Input
            value={state.address1 ?? ""}
            onChange={(e) => update("address1", e.target.value)}
            placeholder={f("addressPlaceholder")}
            readOnly={ro}
            className={ro ? "bg-muted" : undefined}
          />
          {err("address1") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("address1")}
            </p>
          ) : null}
        </div>
        {!quickBooking && (
          <div className="space-y-1">
            <Label>{f("address2")}</Label>
            <Input
              value={state.address2 ?? ""}
              onChange={(e) => update("address2", e.target.value)}
              placeholder={optional}
              readOnly={ro}
              className={ro ? "bg-muted" : undefined}
            />
            {err("address2") ? (
              <p className="text-xs text-destructive" role="alert">
                {err("address2")}
              </p>
            ) : null}
          </div>
        )}
        <div className="space-y-1">
          <Label>{f("postalCode")}</Label>
          <Input
            value={state.postalCode ?? ""}
            onChange={(e) => update("postalCode", e.target.value)}
            placeholder={f("postalCodePlaceholder")}
            readOnly={ro}
            className={ro ? "bg-muted" : undefined}
          />
          {err("postalCode") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("postalCode")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("city")}</Label>
          <Input
            value={state.city ?? ""}
            onChange={(e) => update("city", e.target.value)}
            placeholder={f("cityPlaceholder")}
            readOnly={ro}
            className={ro ? "bg-muted" : undefined}
          />
          {err("city") ? (
            <p className="text-xs text-destructive" role="alert">
              {err("city")}
            </p>
          ) : null}
        </div>
        <div className="space-y-1">
          <Label>{f("country")}</Label>
          <Input
            value={state.country ?? ""}
            onChange={(e) => update("country", e.target.value)}
            placeholder={f("countryPlaceholder")}
            readOnly={ro}
            className={ro ? "bg-muted" : undefined}
          />
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
              <Input
                type="email"
                value={state.email ?? ""}
                onChange={(e) => update("email", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
              {err("email") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("email")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("phone")}</Label>
              <Input
                value={state.phoneNumber ?? ""}
                onChange={(e) => update("phoneNumber", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
              {err("phoneNumber") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("phoneNumber")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("mobile")}</Label>
              <Input
                value={state.phoneNumberMobile ?? ""}
                onChange={(e) => update("phoneNumberMobile", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
              {err("phoneNumberMobile") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("phoneNumberMobile")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("contactPerson")}</Label>
              <Input
                value={state.contactPersonName ?? ""}
                onChange={(e) => update("contactPersonName", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
              {err("contactPersonName") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("contactPersonName")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("vatNo")}</Label>
              <Input
                value={state.vatNo ?? ""}
                onChange={(e) => update("vatNo", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
              {err("vatNo") ? (
                <p className="text-xs text-destructive" role="alert">
                  {err("vatNo")}
                </p>
              ) : null}
            </div>
            <div className="space-y-1">
              <Label>{f("customerNumber")}</Label>
              <Input
                value={state.customerNumber ?? ""}
                onChange={(e) => update("customerNumber", e.target.value)}
                placeholder={optional}
                readOnly={ro}
                className={ro ? "bg-muted" : undefined}
              />
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
