"use client";

import { useAuth } from "@/context/AuthContext";
import { useEffect, useState } from "react";
import {
  adminGetCompanies,
  createCompany,
  type AdminCompany,
} from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function ManageCompaniesPage() {
  const { token } = useAuth();
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [businessId, setBusinessId] = useState("");

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    setError(null);
    adminGetCompanies(token)
      .then((list) => {
        if (!cancelled) setCompanies(list);
      })
      .catch((e) => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  const refetchCompanies = () => {
    if (!token) return;
    adminGetCompanies(token).then(setCompanies).catch(() => {});
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    const companyName = name.trim();
    if (!companyName) {
      setCreateError("Name is required.");
      return;
    }
    setCreating(true);
    setCreateError(null);
    try {
      await createCompany(token, {
        name: companyName,
        businessId: businessId.trim() || undefined,
        companyId: undefined, // backend auto-generates GUID-based companyId when omitted
      });
      setName("");
      setBusinessId("");
      refetchCompanies();
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : "Create failed");
    } finally {
      setCreating(false);
    }
  };

  if (!token) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Companies</h1>
        <p className="text-muted-foreground mt-1">
          View existing companies and create new ones. New users can register with a company Business ID.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create company</CardTitle>
          <CardDescription>
            Add a new company. Name and Business ID align with booking-backend. Company ID and Id (GUID) are auto-generated.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-4">
            <div className="space-y-2">
              <Label htmlFor="company-name">Name *</Label>
              <Input
                id="company-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Acme Oy"
                className="w-[200px]"
                disabled={creating}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="business-id">Business ID</Label>
              <Input
                id="business-id"
                value={businessId}
                onChange={(e) => setBusinessId(e.target.value)}
                placeholder="e.g. FI12345678"
                className="w-[180px]"
                disabled={creating}
              />
            </div>
            <Button type="submit" disabled={creating}>
              {creating ? "Creating…" : "Create company"}
            </Button>
          </form>
          {createError && (
            <p className="mt-3 text-sm text-destructive" role="alert">
              {createError}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Companies</CardTitle>
          <CardDescription>
            All companies in the system. Use Business ID when registering new users.
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
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="p-3 text-left font-medium">Name</th>
                    <th className="p-3 text-left font-medium">Business ID</th>
                    <th className="p-3 text-left font-medium">Company ID (auto)</th>
                    <th className="p-3 text-left font-medium">Id (GUID)</th>
                  </tr>
                </thead>
                <tbody>
                  {companies.map((c) => (
                    <tr key={c.id} className="border-b last:border-0">
                      <td className="p-3">{c.name ?? "—"}</td>
                      <td className="p-3">{c.businessId ?? "—"}</td>
                      <td className="p-3 font-mono text-xs">{c.companyId}</td>
                      <td className="p-3 font-mono text-xs text-muted-foreground">{c.id}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {companies.length === 0 && (
                <p className="p-4 text-muted-foreground">No companies yet. Create one above.</p>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
