'use client';

import { ThemeProvider as NextThemesProvider } from 'next-themes';
import { AuthProvider } from '@/context/AuthContext';
import { BrandingProvider, BrandingDocumentHead } from '@/context/BrandingContext';
import { DesignThemeProvider } from '@/context/DesignThemeContext';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <NextThemesProvider attribute="class" defaultTheme="system" enableSystem>
      <BrandingProvider>
        <BrandingDocumentHead />
        <AuthProvider>
          <DesignThemeProvider>{children}</DesignThemeProvider>
        </AuthProvider>
      </BrandingProvider>
    </NextThemesProvider>
  );
}
