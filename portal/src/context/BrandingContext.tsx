'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import type { BrandingResponse } from '@/lib/api';
import { getBranding } from '@/lib/api';

const defaultBranding: BrandingResponse = {
  appName: 'CargoHub',
  logoUrl: '',
  primaryColor: '',
  secondaryColor: '',
};

const BrandingContext = createContext<BrandingResponse>(defaultBranding);

export function BrandingProvider({ children }: { children: React.ReactNode }) {
  const [branding, setBranding] = useState<BrandingResponse>(defaultBranding);

  useEffect(() => {
    getBranding()
      .then(setBranding)
      .catch(() => setBranding(defaultBranding));
  }, []);

  // Apply theme colors to CSS variables when branding provides them
  useEffect(() => {
    if (typeof document === 'undefined') return;
    const root = document.documentElement;
    if (branding.primaryColor) {
      root.style.setProperty('--primary', branding.primaryColor);
      root.style.setProperty('--sidebar-primary', branding.primaryColor);
    } else {
      root.style.removeProperty('--primary');
      root.style.removeProperty('--sidebar-primary');
    }
    if (branding.secondaryColor) {
      root.style.setProperty('--secondary', branding.secondaryColor);
    } else {
      root.style.removeProperty('--secondary');
    }
  }, [branding.primaryColor, branding.secondaryColor]);

  return (
    <BrandingContext.Provider value={branding}>
      {children}
    </BrandingContext.Provider>
  );
}

export function useBranding(): BrandingResponse {
  const ctx = useContext(BrandingContext);
  return ctx ?? defaultBranding;
}

/** Client component: sets document.title and meta description from branding. Render inside BrandingProvider. */
export function BrandingDocumentHead() {
  const branding = useBranding();

  useEffect(() => {
    if (typeof document === 'undefined') return;
    const title = branding.appName ? `${branding.appName}` : 'CargoHub';
    document.title = title;
    let meta = document.querySelector('meta[name="description"]');
    if (!meta) {
      meta = document.createElement('meta');
      meta.setAttribute('name', 'description');
      document.head.appendChild(meta);
    }
    meta.setAttribute('content', branding.appName ? `${branding.appName} — Booking portal` : 'Booking portal');
  }, [branding.appName]);

  return null;
}
