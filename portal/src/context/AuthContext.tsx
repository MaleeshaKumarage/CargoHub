'use client';

import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import type { LoginResponse } from '@/lib/api';
import { getMe } from '@/lib/api';
import { getRolesFromToken } from '@/lib/jwt';

type AuthState = {
  user: LoginResponse | null;
  token: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
};

type AuthContextValue = AuthState & {
  login: (user: LoginResponse, token: string) => void;
  logout: () => void;
  setLoading: (loading: boolean) => void;
  /** Update current user's roles (e.g. after /me). Saves to storage so Navbar shows Manage Users. */
  updateUserRoles: (roles: string[]) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const STORAGE_KEY = 'portal_auth';

function loadStored(): { user: LoginResponse; token: string } | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { user: LoginResponse; token: string };
    if (!parsed?.user?.userId || !parsed?.token) return null;
    // Normalize roles: from response (roles/Roles) or from JWT payload so Super Admin always sees Manage Users
    const rawRoles = parsed.user.roles ?? (parsed.user as { Roles?: string[] }).Roles;
    let roles: string[] = Array.isArray(rawRoles) ? rawRoles : [];
    if (roles.length === 0) {
      const tokenRoles = getRolesFromToken(parsed.token);
      if (tokenRoles.length > 0) roles = tokenRoles;
    }
    const user: LoginResponse = {
      ...parsed.user,
      roles,
    };
    return { user, token: parsed.token };
  } catch {
    // ignore
  }
  return null;
}

function save(user: LoginResponse, token: string) {
  if (typeof window === 'undefined') return;
  localStorage.setItem(STORAGE_KEY, JSON.stringify({ user, token }));
}

function clear() {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(STORAGE_KEY);
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    token: null,
    isLoading: true,
    isAuthenticated: false,
  });

  useEffect(() => {
    const stored = loadStored();
    setState({
      user: stored?.user ?? null,
      token: stored?.token ?? null,
      isLoading: false,
      isAuthenticated: !!stored?.token,
    });
  }, []);

  // When we have a token but no roles in user, fetch /me so Navbar and Manage Users page work
  useEffect(() => {
    const { token, user } = state;
    if (!token || !user || (user.roles && user.roles.length > 0)) return;
    getMe(token)
      .then((me) => {
        const updatedUser: LoginResponse = { ...user, roles: me.roles };
        save(updatedUser, token);
        setState((prev) => (prev.user ? { ...prev, user: updatedUser } : prev));
      })
      .catch(() => {});
  }, [state.token, state.user?.userId, state.user?.roles?.length]);

  const login = useCallback((user: LoginResponse, token: string) => {
    // Normalize roles: from response (roles/Roles) or from JWT so Super Admin always sees Manage Users
    const rawRoles = user.roles ?? (user as { Roles?: string[] }).Roles;
    let roles: string[] = Array.isArray(rawRoles) ? rawRoles : [];
    if (roles.length === 0) {
      const tokenRoles = getRolesFromToken(token);
      if (tokenRoles.length > 0) roles = tokenRoles;
    }
    const normalizedUser: LoginResponse = {
      ...user,
      roles,
    };
    save(normalizedUser, token);
    setState({
      user: normalizedUser,
      token,
      isLoading: false,
      isAuthenticated: true,
    });
  }, []);

  const logout = useCallback(() => {
    clear();
    setState({
      user: null,
      token: null,
      isLoading: false,
      isAuthenticated: false,
    });
  }, []);

  const setLoading = useCallback((loading: boolean) => {
    setState((prev) => ({ ...prev, isLoading: loading }));
  }, []);

  const updateUserRoles = useCallback((roles: string[]) => {
    setState((prev) => {
      if (!prev.user || !prev.token) return prev;
      const updatedUser: LoginResponse = { ...prev.user, roles };
      save(updatedUser, prev.token);
      return { ...prev, user: updatedUser };
    });
  }, []);

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    setLoading,
    updateUserRoles,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
