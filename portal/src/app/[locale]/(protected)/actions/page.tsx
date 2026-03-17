"use client";

import { useAuth } from "@/context/AuthContext";
import { useRouter } from "@/i18n/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { getAddressBook, addSender, addReceiver, type AddressEntry, type AddressBookResponse } from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const emptyEntry: AddressEntry = {
  name: "",
  address1: "",
  address2: "",
  postalCode: "",
  city: "",
  country: "FI",
  email: "",
  phoneNumber: "",
  contactPersonName: "",
  vatNo: "",
  customerNumber: "",
};

export default function ActionsPage() {
  const t = useTranslations("actions");
  const { token, user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const [listResponse, setListResponse] = useState<{ addressBooks: AddressBookResponse[] } | null>(null);
  const [selectedCompanyId, setSelectedCompanyId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [senderForm, setSenderForm] = useState<AddressEntry>(emptyEntry);
  const [receiverForm, setReceiverForm] = useState<AddressEntry>(emptyEntry);
  const [addingSender, setAddingSender] = useState(false);
  const [addingReceiver, setAddingReceiver] = useState(false);

  const roles = user?.roles ?? [];
  const isSuperAdmin = Array.isArray(roles) && roles.includes("SuperAdmin");
  const addressBooks = listResponse?.addressBooks ?? [];
  const selectedBook: AddressBookResponse | null =
    selectedCompanyId
      ? addressBooks.find((b) => b.companyId === selectedCompanyId) ?? addressBooks[0] ?? null
      : addressBooks[0] ?? null;
  const data = selectedBook;

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!token) return;
    setLoading(true);
    setError(null);
    getAddressBook(token)
      .then((res) => {
        setListResponse(res);
        if (isSuperAdmin && !selectedCompanyId && res.addressBooks?.length)
          setSelectedCompanyId(res.addressBooks[0].companyId);
      })
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [token, isAuthenticated, isLoading, router, isSuperAdmin]);

  function isDuplicateEntry(newEntry: AddressEntry, existingList: AddressEntry[]): boolean {
    return existingList.some((existing) => 
      existing.name.trim().toLowerCase() === newEntry.name.trim().toLowerCase() &&
      existing.address1.trim().toLowerCase() === newEntry.address1.trim().toLowerCase() &&
      (existing.address2 ?? "").trim().toLowerCase() === (newEntry.address2 ?? "").trim().toLowerCase() &&
      existing.postalCode.trim().toLowerCase() === newEntry.postalCode.trim().toLowerCase() &&
      existing.city.trim().toLowerCase() === newEntry.city.trim().toLowerCase() &&
      existing.country.trim().toLowerCase() === newEntry.country.trim().toLowerCase() &&
      (existing.email ?? "").trim().toLowerCase() === (newEntry.email ?? "").trim().toLowerCase() &&
      (existing.phoneNumber ?? "").trim() === (newEntry.phoneNumber ?? "").trim() &&
      (existing.contactPersonName ?? "").trim().toLowerCase() === (newEntry.contactPersonName ?? "").trim().toLowerCase() &&
      (existing.vatNo ?? "").trim().toLowerCase() === (newEntry.vatNo ?? "").trim().toLowerCase() &&
      (existing.customerNumber ?? "").trim().toLowerCase() === (newEntry.customerNumber ?? "").trim().toLowerCase()
    );
  }

  async function handleAddSender(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setAddingSender(true);
    setError(null);
    try {
      if (data && isDuplicateEntry(senderForm, data.senders)) {
        setError(t("alreadyExists"));
        return;
      }
      await addSender(token, senderForm, isSuperAdmin ? selectedCompanyId : undefined);
      const updated = await getAddressBook(token);
      setListResponse(updated);
      setSenderForm(emptyEntry);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to add sender");
    } finally {
      setAddingSender(false);
    }
  }

  async function handleAddReceiver(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setAddingReceiver(true);
    setError(null);
    try {
      if (data && isDuplicateEntry(receiverForm, data.receivers)) {
        setError(t("alreadyExists"));
        return;
      }
      await addReceiver(token, receiverForm, isSuperAdmin ? selectedCompanyId : undefined);
      const updated = await getAddressBook(token);
      setListResponse(updated);
      setReceiverForm(emptyEntry);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to add receiver");
    } finally {
      setAddingReceiver(false);
    }
  }

  if (!isAuthenticated || isLoading) return null;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
      <Card>
        <CardHeader>
          <CardTitle>{t("addressBook")}</CardTitle>
          <CardDescription>{t("addressBookDescription")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {isSuperAdmin && addressBooks.length > 1 && (
            <div className="space-y-2">
              <Label className="text-sm">{t("filterByCompany")}</Label>
              <select
                className="w-full max-w-md rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={selectedCompanyId ?? ""}
                onChange={(e) => setSelectedCompanyId(e.target.value || null)}
              >
                {addressBooks.map((ab) => (
                  <option key={ab.companyId} value={ab.companyId}>
                    {ab.companyName || ab.companyId}
                  </option>
                ))}
              </select>
            </div>
          )}
          {data?.companyName && (
            <p className="text-sm text-muted-foreground">
              {t("company")}: <span className="font-medium">{data.companyName}</span>
            </p>
          )}
          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}
          {loading ? (
            <p className="text-muted-foreground">{t("loading")}</p>
          ) : !data ? (
            <p className="text-muted-foreground">{t("noCompany")}</p>
          ) : (
            <div className="space-y-6">
              {/* Add Forms - hidden for SuperAdmin (view-only address books) */}
              {!isSuperAdmin && (
              <div className="grid gap-6 md:grid-cols-2">
                {/* Add Sender Form */}
                <form onSubmit={handleAddSender} className="space-y-3 rounded-lg border p-4 bg-muted/20">
                  <h2 className="text-lg font-semibold">{t("addSender")}</h2>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <div>
                      <Label className="text-xs">{t("name")}</Label>
                      <Input
                        value={senderForm.name ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, name: e.target.value }))}
                        placeholder={t("placeholderName")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("address1")}</Label>
                      <Input
                        value={senderForm.address1 ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, address1: e.target.value }))}
                        placeholder={t("placeholderAddress")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("postalCode")}</Label>
                      <Input
                        value={senderForm.postalCode ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, postalCode: e.target.value }))}
                        placeholder={t("placeholderPostalCode")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("city")}</Label>
                      <Input
                        value={senderForm.city ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, city: e.target.value }))}
                        placeholder={t("placeholderCity")}
                      />
                    </div>
                    <div className="sm:col-span-2">
                      <Label className="text-xs">{t("country")}</Label>
                      <Input
                        value={senderForm.country ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, country: e.target.value }))}
                        placeholder={t("placeholderCountry")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("email")}</Label>
                      <Input
                        type="email"
                        value={senderForm.email ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, email: e.target.value }))}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("phone")}</Label>
                      <Input
                        value={senderForm.phoneNumber ?? ""}
                        onChange={(e) => setSenderForm((f) => ({ ...f, phoneNumber: e.target.value }))}
                      />
                    </div>
                  </div>
                  <Button type="submit" disabled={addingSender}>
                    {addingSender ? t("adding") : t("addSender")}
                  </Button>
                </form>

                {/* Add Receiver Form */}
                <form onSubmit={handleAddReceiver} className="space-y-3 rounded-lg border p-4 bg-muted/20">
                  <h2 className="text-lg font-semibold">{t("addReceiver")}</h2>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <div>
                      <Label className="text-xs">{t("name")}</Label>
                      <Input
                        value={receiverForm.name ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, name: e.target.value }))}
                        placeholder={t("placeholderName")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("address1")}</Label>
                      <Input
                        value={receiverForm.address1 ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, address1: e.target.value }))}
                        placeholder={t("placeholderAddress")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("postalCode")}</Label>
                      <Input
                        value={receiverForm.postalCode ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, postalCode: e.target.value }))}
                        placeholder={t("placeholderPostalCode")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("city")}</Label>
                      <Input
                        value={receiverForm.city ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, city: e.target.value }))}
                        placeholder={t("placeholderCity")}
                      />
                    </div>
                    <div className="sm:col-span-2">
                      <Label className="text-xs">{t("country")}</Label>
                      <Input
                        value={receiverForm.country ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, country: e.target.value }))}
                        placeholder={t("placeholderCountry")}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("email")}</Label>
                      <Input
                        type="email"
                        value={receiverForm.email ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, email: e.target.value }))}
                      />
                    </div>
                    <div>
                      <Label className="text-xs">{t("phone")}</Label>
                      <Input
                        value={receiverForm.phoneNumber ?? ""}
                        onChange={(e) => setReceiverForm((f) => ({ ...f, phoneNumber: e.target.value }))}
                      />
                    </div>
                  </div>
                  <Button type="submit" disabled={addingReceiver}>
                    {addingReceiver ? t("adding") : t("addReceiver")}
                  </Button>
                </form>
              </div>
              )}

              {/* Address Book Lists - Bottom Section */}
              <div className="grid gap-8 md:grid-cols-2">
                {/* Senders List */}
                <div className="space-y-4">
                  <h2 className="text-lg font-semibold">{t("senders")}</h2>
                  <table className="w-full text-sm border rounded-md">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="p-2 text-left font-medium">{t("name")}</th>
                        <th className="p-2 text-left font-medium">{t("city")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.senders.length === 0 ? (
                        <tr>
                          <td colSpan={2} className="p-3 text-muted-foreground">
                            {t("noSenders")}
                          </td>
                        </tr>
                      ) : (
                        data.senders.map((s) => (
                          <tr key={s.id ?? s.name} className="border-b">
                            <td className="p-2">{s.name || "—"}</td>
                            <td className="p-2">{s.city || "—"}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Receivers List */}
                <div className="space-y-4">
                  <h2 className="text-lg font-semibold">{t("receivers")}</h2>
                  <table className="w-full text-sm border rounded-md">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="p-2 text-left font-medium">{t("name")}</th>
                        <th className="p-2 text-left font-medium">{t("city")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.receivers.length === 0 ? (
                        <tr>
                          <td colSpan={2} className="p-3 text-muted-foreground">
                            {t("noReceivers")}
                          </td>
                        </tr>
                      ) : (
                        data.receivers.map((r) => (
                          <tr key={r.id ?? r.name} className="border-b">
                            <td className="p-2">{r.name || "—"}</td>
                            <td className="p-2">{r.city || "—"}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
