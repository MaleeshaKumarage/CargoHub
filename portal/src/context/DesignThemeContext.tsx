'use client';

import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { getMe, updateTheme, type DesignTheme } from '@/lib/api';
import { useAuth } from '@/context/AuthContext';

const DEFAULT_THEME: DesignTheme = 'minimalism';

type DesignThemeContextValue = {
  theme: DesignTheme;
  setTheme: (theme: DesignTheme) => void;
  isLoading: boolean;
};

const DesignThemeContext = createContext<DesignThemeContextValue | null>(null);

function applyThemeToDocument(theme: DesignTheme) {
  if (typeof document === 'undefined') return;
  document.documentElement.setAttribute('data-theme', theme);
}

export function DesignThemeProvider({ children }: { children: React.ReactNode }) {
  const { token, isAuthenticated } = useAuth();
  const [theme, setThemeState] = useState<DesignTheme>(DEFAULT_THEME);
  const [isLoading, setIsLoading] = useState(true);

  // Fetch theme from API when user is logged in
  useEffect(() => {
    if (!token || !isAuthenticated) {
      setThemeState(DEFAULT_THEME);
      applyThemeToDocument(DEFAULT_THEME);
      setIsLoading(false);
      return;
    }
    getMe(token)
      .then((me) => {
        const t = (me.theme ?? DEFAULT_THEME).toLowerCase();
        const valid: DesignTheme =
          t === 'skeuomorphism' || t === 'neobrutalism' || t === 'claymorphism' || t === 'minimalism'
            ? t
            : DEFAULT_THEME;
        setThemeState(valid);
        applyThemeToDocument(valid);
      })
      .catch(() => {
        setThemeState(DEFAULT_THEME);
        applyThemeToDocument(DEFAULT_THEME);
      })
      .finally(() => setIsLoading(false));
  }, [token, isAuthenticated]);

  const setTheme = useCallback(
    (newTheme: DesignTheme) => {
      setThemeState(newTheme);
      applyThemeToDocument(newTheme);
      if (token) {
        updateTheme(token, newTheme).catch(() => {
          // On failure, refetch to restore server state
          getMe(token).then((me) => {
            const t = (me.theme ?? DEFAULT_THEME).toLowerCase();
            const valid: DesignTheme =
              t === 'skeuomorphism' || t === 'neobrutalism' || t === 'claymorphism' || t === 'minimalism'
                ? t
                : DEFAULT_THEME;
            setThemeState(valid);
            applyThemeToDocument(valid);
          });
        });
      }
    },
    [token]
  );

  const value: DesignThemeContextValue = {
    theme,
    setTheme,
    isLoading,
  };

  return (
    <DesignThemeContext.Provider value={value}>{children}</DesignThemeContext.Provider>
  );
}

export function useDesignTheme() {
  const ctx = useContext(DesignThemeContext);
  if (!ctx) throw new Error('useDesignTheme must be used within DesignThemeProvider');
  return ctx;
}
