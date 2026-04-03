/**
 * API base URL. In Docker + nginx + single ngrok URL, use __SAME_ORIGIN__ so the browser
 * calls /api on the same host as the UI. SSR uses 127.0.0.1:8080 inside the container.
 */
function getApiUrl(): string {
  const env = process.env.NEXT_PUBLIC_API_URL;
  if (typeof window !== 'undefined') {
    if (!env || env === '__SAME_ORIGIN__') {
      return window.location.origin;
    }
    return env;
  }
  if (!env || env === '__SAME_ORIGIN__') {
    return 'http://127.0.0.1:8080';
  }
  return env;
}

function portalBase(): string {
  return `${getApiUrl()}/api/v1/portal`;
}
function adminBase(): string {
  return `${getApiUrl()}/api/v1/admin`;
}
function companyBase(): string {
  return `${getApiUrl()}/api/v1/company`;
}

export type LoginResponse = {
  userId: string;
  email: string;
  displayName: string;
  businessId?: string | null;
  customerMappingId?: string | null;
  jwtToken: string;
  /** Role names from API (e.g. SuperAdmin, Admin, User). Default to [] if missing. */
  roles?: string[];
};

export type LoginResult = {
  success: boolean;
  errorCode?: string | null;
  message?: string | null;
  data?: LoginResponse | null;
};

export type RegisterBody = {
  email: string;
  password: string;
  userName: string;
  businessId?: string | null;
  gsOne?: string | null;
};

export async function login(account: string, password: string): Promise<LoginResult> {
  const res = await fetch(`${portalBase()}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ account, password }),
  });
  const data = await res.json();
  if (!res.ok) {
    return { success: false, errorCode: data.errorCode ?? 'Error', message: data.message ?? 'Login failed' };
  }
  if (data.success === false) {
    return { success: false, errorCode: data.errorCode, message: data.message };
  }
  // Normalize so UI always gets roles array and jwtToken (API may send "roles"/"Roles", "jwtToken"/"JwtToken")
  const rawRoles = data.roles ?? data.Roles;
  const normalizedData: LoginResponse = {
    userId: data.userId,
    email: data.email,
    displayName: data.displayName,
    businessId: data.businessId ?? null,
    customerMappingId: data.customerMappingId ?? null,
    jwtToken: data.jwtToken ?? data.JwtToken ?? '',
    roles: Array.isArray(rawRoles) ? rawRoles : [],
  };
  return { success: true, data: normalizedData };
}

export async function register(body: RegisterBody): Promise<LoginResult> {
  const res = await fetch(`${portalBase()}/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  let data: Record<string, unknown> = {};
  try {
    data = (await res.json()) as Record<string, unknown>;
  } catch {
    if (!res.ok) {
      return {
        success: false,
        errorCode: 'Error',
        message: res.statusText?.trim() ? res.statusText : 'Registration failed',
      };
    }
    return { success: false, message: 'Invalid response' };
  }
  if (!res.ok) {
    return {
      success: false,
      errorCode: (data.errorCode as string) ?? 'Error',
      message: (data.message as string) ?? (data.title as string) ?? 'Registration failed',
    };
  }
  const userId = (data.userId ?? data.UserId) as string | undefined;
  const jwtToken = (data.jwtToken ?? data.JwtToken ?? '') as string;
  if (userId && jwtToken) {
    const rawRoles = data.roles ?? data.Roles;
    const normalizedData: LoginResponse = {
      userId,
      email: (data.email as string) ?? '',
      displayName: (data.displayName as string) ?? '',
      businessId: (data.businessId ?? data.BusinessId ?? null) as string | null,
      customerMappingId: (data.customerMappingId ?? data.CustomerMappingId ?? null) as string | null,
      jwtToken,
      roles: Array.isArray(rawRoles) ? (rawRoles as string[]) : [],
    };
    return { success: true, data: normalizedData };
  }
  return { success: false, message: 'Invalid response' };
}

export type AuthResult = { success: boolean; message?: string | null };

export async function requestPasswordReset(email: string): Promise<AuthResult> {
  const res = await fetch(`${portalBase()}/requestPasswordReset`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    return { success: false, message: data.message ?? 'Request failed' };
  }
  return { success: true };
}

export async function resetPassword(token: string, newPassword: string): Promise<AuthResult> {
  const res = await fetch(`${portalBase()}/resetPassword`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, newPassword }),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    return { success: false, message: data.message ?? 'Reset failed' };
  }
  return { success: true };
}

/** Deployment branding (GET /api/v1/portal/branding). No auth required. */
export type BrandingResponse = {
  appName: string;
  logoUrl: string;
  primaryColor: string;
  secondaryColor: string;
};

export async function getBranding(): Promise<BrandingResponse> {
  const res = await fetch(`${portalBase()}/branding`);
  if (!res.ok) {
    return { appName: 'Portal', logoUrl: '', primaryColor: '', secondaryColor: '' };
  }
  const data = await res.json();
  return {
    appName: data.appName ?? 'Portal',
    logoUrl: data.logoUrl ?? '',
    primaryColor: data.primaryColor ?? '',
    secondaryColor: data.secondaryColor ?? '',
  };
}

/** Current user and roles from JWT (GET /api/v1/portal/me). Includes company businessId and companyName when user is linked to a company. */
export type PortalMeResponse = {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
  businessId?: string | null;
  companyName?: string | null;
  /** UI design theme (skeuomorphism, neobrutalism, claymorphism, minimalism). */
  theme?: string | null;
};

export async function getMe(token: string): Promise<PortalMeResponse> {
  const res = await fetch(`${portalBase()}/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error('Failed to load current user');
  const data = await res.json();
  const rawRoles = data.roles ?? data.Roles;
  return {
    userId: data.userId,
    email: data.email,
    displayName: data.displayName ?? '',
    roles: Array.isArray(rawRoles) ? rawRoles : [],
    businessId: data.businessId ?? data.BusinessId ?? null,
    companyName: data.companyName ?? data.CompanyName ?? null,
    theme: data.theme ?? data.Theme ?? null,
  };
}

/** Valid design theme values. */
export const DESIGN_THEMES = ['skeuomorphism', 'neobrutalism', 'claymorphism', 'minimalism'] as const;
export type DesignTheme = (typeof DESIGN_THEMES)[number];

/** Update user theme preference (PATCH /api/v1/portal/me/preferences). */
export async function updateTheme(token: string, theme: string): Promise<void> {
  const res = await fetch(`${portalBase()}/me/preferences`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify({ theme }),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error((data as { message?: string }).message ?? 'Failed to update theme');
  }
}

/** Registered courier IDs for the booking form dropdown (GET /api/v1/portal/couriers). */
export async function getCouriers(token: string): Promise<string[]> {
  const res = await fetch(`${portalBase()}/couriers`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error('Failed to load couriers');
  const data = await res.json();
  const ids = data.courierIds ?? data.CourierIds ?? [];
  return Array.isArray(ids) ? ids : [];
}

// --- Admin API (Super Admin only; pass JWT from auth) ---

export type AdminCompany = {
  id: string;
  name?: string | null;
  businessId?: string | null;
  companyId: string;
  maxUserAccounts?: number | null;
  maxAdminAccounts?: number | null;
  initialAdminInviteEmail?: string | null;
  activeUserCount?: number;
  adminCount?: number;
};

export type AdminCreateCompanyBody = {
  name: string;
  businessId: string;
  maxUserAccounts?: number | null;
  maxAdminAccounts?: number | null;
  initialAdminEmail?: string | null;
};

export type AdminPatchCompanyBody = {
  maxUserAccounts?: number | null;
  maxAdminAccounts?: number | null;
  resendAdminInvite?: boolean;
};
export type AdminUser = {
  userId: string;
  email: string;
  displayName: string;
  businessId?: string | null;
  isActive: boolean;
  roles: string[];
};

export async function adminGetCompanies(token: string): Promise<AdminCompany[]> {
  const res = await fetch(`${adminBase()}/companies`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    const msg = (data as { message?: string }).message ?? (data as { title?: string }).title ?? res.statusText;
    throw new Error(`Failed to load companies (${res.status}): ${msg}`);
  }
  return res.json();
}

export async function adminCreateCompany(token: string, body: AdminCreateCompanyBody): Promise<AdminCompany> {
  const res = await fetch(`${adminBase()}/companies`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const msg =
      (data as { message?: string }).message ?? (data as { title?: string }).title ?? res.statusText;
    throw new Error(msg || `Create failed (${res.status})`);
  }
  return data as AdminCompany;
}

export async function adminPatchCompany(
  token: string,
  companyId: string,
  body: AdminPatchCompanyBody
): Promise<AdminCompany> {
  const res = await fetch(`${adminBase()}/companies/${encodeURIComponent(companyId)}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const msg =
      (data as { message?: string }).message ?? (data as { title?: string }).title ?? res.statusText;
    throw new Error(msg || `Update failed (${res.status})`);
  }
  return data as AdminCompany;
}

/** Anonymous: accept Super Admin–sent company admin invite (returns JWT like register). */
export async function acceptCompanyAdminInvite(body: {
  token: string;
  password: string;
  userName: string;
}): Promise<LoginResult> {
  const res = await fetch(`${portalBase()}/accept-company-admin-invite`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  let data: Record<string, unknown> = {};
  try {
    data = (await res.json()) as Record<string, unknown>;
  } catch {
    return { success: false, message: 'Invalid response' };
  }
  if (!res.ok) {
    return {
      success: false,
      errorCode: (data.errorCode as string) ?? 'Error',
      message: (data.message as string) ?? 'Request failed',
    };
  }
  const userId = (data.userId ?? data.UserId) as string | undefined;
  const jwtToken = (data.jwtToken ?? data.JwtToken ?? '') as string;
  if (userId && jwtToken) {
    const rawRoles = data.roles ?? data.Roles;
    const normalizedData: LoginResponse = {
      userId,
      email: (data.email as string) ?? '',
      displayName: (data.displayName as string) ?? '',
      businessId: (data.businessId ?? data.BusinessId ?? null) as string | null,
      customerMappingId: (data.customerMappingId ?? data.CustomerMappingId ?? null) as string | null,
      jwtToken,
      roles: Array.isArray(rawRoles) ? (rawRoles as string[]) : [],
    };
    return { success: true, data: normalizedData };
  }
  return { success: false, message: 'Invalid response' };
}

export async function adminGetUsers(token: string, businessId?: string | null): Promise<AdminUser[]> {
  const url = businessId ? `${adminBase()}/users?businessId=${encodeURIComponent(businessId)}` : `${adminBase()}/users`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (!res.ok) throw new Error('Failed to load users');
  return res.json();
}

export async function adminPatchUser(
  token: string,
  userId: string,
  body: { role?: string; isActive?: boolean }
): Promise<void> {
  const res = await fetch(`${adminBase()}/users/${encodeURIComponent(userId)}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error((data as { message?: string }).message ?? 'Update failed');
  }
}

// --- Company API (create company; JWT required) ---

export type CreateCompanyBody = {
  name?: string | null;
  businessId?: string | null;
  companyId?: string | null;
  senderNumber?: string | null;
  divisionCode?: string | null;
};

export type CompanyCreated = {
  id: string;
  companyId: string;
  name?: string | null;
  businessId?: string | null;
  customerId?: string | null;
  senderNumber?: string | null;
  divisionCode?: string | null;
};

export async function createCompany(token: string, body: CreateCompanyBody): Promise<CompanyCreated> {
  const res = await fetch(companyBase(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    const msg = (data as { message?: string }).message ?? (data as { title?: string }).title ?? res.statusText;
    throw new Error(`Create company failed (${res.status}): ${msg}`);
  }
  return res.json();
}

// --- Portal Company Address Book (JWT required; per company, not shared) ---

export type AddressEntry = {
  id?: string | null;
  name?: string | null;
  address1?: string | null;
  address2?: string | null;
  postalCode?: string | null;
  city?: string | null;
  country?: string | null;
  phoneNumber?: string | null;
  contactPersonName?: string | null;
  email?: string | null;
  county?: string | null;
  vatNo?: string | null;
  customerNumber?: string | null;
};

export type AddressBookResponse = {
  companyId: string;
  companyName?: string | null;
  senders: AddressEntry[];
  receivers: AddressEntry[];
};

/** Response from GET address-book. User/Admin: one item; SuperAdmin: all companies or filtered. */
export type AddressBookListResponse = {
  addressBooks: AddressBookResponse[];
};

export async function getAddressBook(
  token: string,
  options?: { companyId?: string }
): Promise<AddressBookListResponse> {
  const url = new URL(`${portalBase()}/company/address-book`);
  if (options?.companyId) url.searchParams.set('companyId', options.companyId);
  const res = await fetch(url.toString(), {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    if (res.status === 404) throw new Error('Company not found. Your account may not be linked to a company.');
    throw new Error('Failed to load address book');
  }
  return res.json();
}

export async function addSender(token: string, entry: AddressEntry, companyId?: string | null): Promise<AddressEntry> {
  const url = new URL(`${portalBase()}/company/senders`);
  if (companyId) url.searchParams.set('companyId', companyId);
  const res = await fetch(url.toString(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(entry),
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({})) as { message?: string };
    throw new Error(body.message ?? 'Failed to add sender');
  }
  return res.json();
}

export async function addReceiver(token: string, entry: AddressEntry, companyId?: string | null): Promise<AddressEntry> {
  const url = new URL(`${portalBase()}/company/receivers`);
  if (companyId) url.searchParams.set('companyId', companyId);
  const res = await fetch(url.toString(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(entry),
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({})) as { message?: string };
    throw new Error(body.message ?? 'Failed to add receiver');
  }
  return res.json();
}

// --- Portal Bookings (JWT required) ---

export type BookingListItem = {
  id: string;
  shipmentNumber?: string | null;
  waybillNumber?: string | null;
  customerName?: string | null;
  createdAtUtc: string;
  enabled: boolean;
  isFavourite: boolean;
  isDraft?: boolean;
  /** From status history table; used by milestone bar on list and detail. */
  statusHistory?: { status: string; occurredAtUtc: string; source?: string | null }[];
};

/** Party as returned in booking detail */
export type BookingDetailParty = {
  name: string;
  address1: string;
  address2?: string | null;
  postalCode: string;
  city: string;
  country: string;
  email?: string | null;
  phoneNumber?: string | null;
  phoneNumberMobile?: string | null;
  contactPersonName?: string | null;
  vatNo?: string | null;
  customerNumber?: string | null;
};

export type BookingDetailShipment = {
  service?: string | null;
  senderReference?: string | null;
  receiverReference?: string | null;
  freightPayer?: string | null;
  handlingInstructions?: string | null;
};

export type BookingDetailShippingInfo = {
  grossWeight?: string | null;
  grossVolume?: string | null;
  packageQuantity?: string | null;
  pickupHandlingInstructions?: string | null;
  deliveryHandlingInstructions?: string | null;
  generalInstructions?: string | null;
  deliveryWithoutSignature?: boolean;
};

export type BookingDetailPackage = {
  id: number;
  weight?: string | null;
  volume?: string | null;
  packageType?: string | null;
  description?: string | null;
  length?: string | null;
  width?: string | null;
  height?: string | null;
};

export type BookingDetail = {
  id: string;
  customerId: string;
  shipmentNumber?: string | null;
  waybillNumber?: string | null;
  customerName?: string | null;
  enabled: boolean;
  isTestBooking: boolean;
  isFavourite: boolean;
  isDraft?: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  header: { senderId: string; companyId?: string | null; referenceNumber?: string | null; postalService?: string | null };
  shipper?: BookingDetailParty | null;
  receiver?: BookingDetailParty | null;
  payer?: BookingDetailParty | null;
  pickUpAddress?: BookingDetailParty | null;
  deliveryPoint?: BookingDetailParty | null;
  shipment?: BookingDetailShipment | null;
  shippingInfo?: BookingDetailShippingInfo | null;
  packages?: BookingDetailPackage[];
  /** When each milestone status was reached (from status history table). */
  statusHistory?: BookingStatusEvent[];
};

export type BookingStatusEvent = {
  status: string;
  occurredAtUtc: string;
  source?: string | null;
};

/** Party/address for shipper, receiver, payer, pickup, delivery */
export type CreateBookingParty = {
  name?: string | null;
  address1?: string | null;
  address2?: string | null;
  postalCode?: string | null;
  city?: string | null;
  country?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  phoneNumberMobile?: string | null;
  contactPersonName?: string | null;
  vatNo?: string | null;
  customerNumber?: string | null;
};

export type CreateBookingShipment = {
  service?: string | null;
  senderReference?: string | null;
  receiverReference?: string | null;
  freightPayer?: string | null;
  handlingInstructions?: string | null;
};

/** A single package in a booking. A booking can have multiple packages. */
export type CreateBookingPackage = {
  weight?: string | null;
  volume?: string | null;
  packageType?: string | null;
  description?: string | null;
  length?: string | null;
  width?: string | null;
  height?: string | null;
};

export type CreateBookingShippingInfo = {
  grossWeight?: string | null;
  grossVolume?: string | null;
  packageQuantity?: string | null;
  pickupHandlingInstructions?: string | null;
  deliveryHandlingInstructions?: string | null;
  generalInstructions?: string | null;
  deliveryWithoutSignature?: boolean | null;
  packages?: CreateBookingPackage[] | null;
};

export type CreateBookingBody = {
  referenceNumber?: string | null;
  postalService?: string | null;
  companyId?: string | null;
  receiverName?: string | null;
  receiverAddress1?: string | null;
  receiverAddress2?: string | null;
  receiverPostalCode?: string | null;
  receiverCity?: string | null;
  receiverCountry?: string | null;
  receiverEmail?: string | null;
  receiverPhone?: string | null;
  receiverPhoneMobile?: string | null;
  receiverContactPersonName?: string | null;
  receiverVatNo?: string | null;
  receiverCustomerNumber?: string | null;
  shipper?: CreateBookingParty | null;
  payer?: CreateBookingParty | null;
  pickUpAddress?: CreateBookingParty | null;
  deliveryPoint?: CreateBookingParty | null;
  shipment?: CreateBookingShipment | null;
  shippingInfo?: CreateBookingShippingInfo | null;
};

export async function bookingList(token: string, skip = 0, take = 100): Promise<BookingListItem[]> {
  const res = await fetch(`${portalBase()}/bookings?skip=${skip}&take=${take}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error('Failed to load bookings');
  return res.json();
}

export async function bookingGet(token: string, id: string): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    if (res.status === 404) throw new Error('Booking not found');
    throw new Error('Failed to load booking');
  }
  return res.json();
}

/** Fetches waybill PDF for a completed booking. Returns a blob URL to open/print. Revoke with URL.revokeObjectURL after use. */
export async function getWaybillPdfBlobUrl(token: string, bookingId: string): Promise<string> {
  const res = await fetch(`${portalBase()}/bookings/${bookingId}/waybill`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    const msg = (data as { message?: string }).message ?? res.statusText;
    throw new Error(msg || 'Failed to load waybill');
  }
  const blob = await res.blob();
  return URL.createObjectURL(blob);
}

function apiError(res: Response, data: Record<string, unknown>, fallback: string): string {
  const msg =
    (data.detail as string) ??
    (data.title as string) ??
    (data.message as string) ??
    (data as { message?: string }).message;
  const status = res.status;
  if (msg) return status >= 500 ? `${msg} (${status})` : msg;
  return status === 400 ? 'Invalid request. Check receiver details.' : fallback;
}

export async function bookingCreate(token: string, body: CreateBookingBody): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
    const message = apiError(res, data, 'Create failed');
    if (message.includes('IsDraft') || message.includes('column') || res.status === 500) {
      throw new Error(`${message} If this persists, apply the database migration (Scope/apply-IsDraft-migration.sql).`);
    }
    throw new Error(message);
  }
  return res.json();
}

// --- Drafts: save as draft, retrieve, fill rest, confirm to complete ---

export async function draftList(token: string, skip = 0, take = 100): Promise<BookingListItem[]> {
  const res = await fetch(`${portalBase()}/bookings/draft?skip=${skip}&take=${take}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error('Failed to load drafts');
  return res.json();
}

export async function draftGet(token: string, id: string): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings/draft/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    if (res.status === 404) throw new Error('Draft not found');
    throw new Error('Failed to load draft');
  }
  return res.json();
}

export async function draftCreate(token: string, body: CreateBookingBody): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings/draft`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error((data as { message?: string }).message ?? 'Save draft failed');
  }
  return res.json();
}

export type UpdateDraftBody = CreateBookingBody;

export async function draftUpdate(token: string, id: string, body: UpdateDraftBody): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings/draft/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    if (res.status === 404) throw new Error('Draft not found');
    const data = await res.json().catch(() => ({}));
    throw new Error((data as { message?: string }).message ?? 'Update failed');
  }
  return res.json();
}

export async function draftConfirm(token: string, id: string): Promise<BookingDetail> {
  const res = await fetch(`${portalBase()}/bookings/draft/${id}/confirm`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    if (res.status === 404) throw new Error('Draft not found');
    const data = await res.json().catch(() => ({}));
    throw new Error((data as { message?: string }).message ?? 'Confirm failed');
  }
  return res.json();
}

export type BookingImportResult = {
  createdCount: number;
  draftCount: number;
  errors: string[];
  createdBookingIds: string[];
  draftBookingIds: string[];
};

function parseImportResult(data: Record<string, unknown>): BookingImportResult {
  const ids = (k: string) =>
    Array.isArray(data[k]) ? (data[k] as unknown[]).map((x) => String(x)) : [];
  return {
    createdCount: Number(data.createdCount ?? 0),
    draftCount: Number(data.draftCount ?? 0),
    errors: Array.isArray(data.errors) ? (data.errors as string[]) : [],
    createdBookingIds: ids("createdBookingIds"),
    draftBookingIds: ids("draftBookingIds"),
  };
}

/** Upload CSV or Excel; headers must match export format (single-step import). */
export async function bookingsImport(token: string, file: File): Promise<BookingImportResult> {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(`${portalBase()}/bookings/import`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    const msg =
      (typeof data.message === "string" && data.message) ||
      (typeof data.title === "string" && data.title) ||
      "Import failed";
    throw new Error(msg);
  }
  return parseImportResult(data);
}

export type BookingImportPreview = {
  sessionId: string;
  completedCount: number;
  draftCount: number;
  skippedEmptyRows: number;
  totalDataRows: number;
};

export type BookingImportAnalyze = {
  needsMapping: boolean;
  sessionId: string;
  completedCount: number;
  draftCount: number;
  skippedEmptyRows: number;
  totalDataRows: number;
  fileHeaders: string[];
  bookingFields: string[];
  hasSavedMapping: boolean;
  savedColumnMap: Record<string, string | null> | null;
};

/** Analyze file: exact template → counts + session; otherwise session + headers for column mapping. */
export async function bookingsImportAnalyze(token: string, file: File): Promise<BookingImportAnalyze> {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(`${portalBase()}/bookings/import/analyze`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    const msg =
      (typeof data.message === "string" && data.message) ||
      (typeof data.title === "string" && data.title) ||
      "Import analyze failed";
    throw new Error(msg);
  }
  let savedColumnMap: Record<string, string | null> | null = null;
  const rawSaved = data.savedColumnMap;
  if (rawSaved && typeof rawSaved === "object" && !Array.isArray(rawSaved)) {
    savedColumnMap = {};
    for (const [k, v] of Object.entries(rawSaved as Record<string, unknown>)) {
      if (v === null || v === undefined || v === "") savedColumnMap[k] = null;
      else savedColumnMap[k] = String(v);
    }
  }

  return {
    needsMapping: Boolean(data.needsMapping),
    sessionId: String(data.sessionId ?? ""),
    completedCount: Number(data.completedCount ?? 0),
    draftCount: Number(data.draftCount ?? 0),
    skippedEmptyRows: Number(data.skippedEmptyRows ?? 0),
    totalDataRows: Number(data.totalDataRows ?? 0),
    fileHeaders: Array.isArray(data.fileHeaders) ? data.fileHeaders.map((x) => String(x)) : [],
    bookingFields: Array.isArray(data.bookingFields) ? data.bookingFields.map((x) => String(x)) : [],
    hasSavedMapping: Boolean(data.hasSavedMapping),
    savedColumnMap,
  };
}

/** Apply column mapping after analyze (needsMapping); returns same shape as preview for confirm. */
export async function bookingsImportApplyMapping(
  token: string,
  body: {
    sessionId: string;
    columnMap: Record<string, string | null>;
    saveMappingForCompany?: boolean;
  },
): Promise<BookingImportPreview> {
  const res = await fetch(`${portalBase()}/bookings/import/apply-mapping`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      sessionId: body.sessionId,
      columnMap: body.columnMap,
      saveMappingForCompany: Boolean(body.saveMappingForCompany),
    }),
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    const msg =
      (typeof data.message === "string" && data.message) ||
      (typeof data.title === "string" && data.title) ||
      "Import mapping failed";
    throw new Error(msg);
  }
  return {
    sessionId: String(data.sessionId ?? ""),
    completedCount: Number(data.completedCount ?? 0),
    draftCount: Number(data.draftCount ?? 0),
    skippedEmptyRows: Number(data.skippedEmptyRows ?? 0),
    totalDataRows: Number(data.totalDataRows ?? 0),
  };
}

/** Parse file and return counts; server holds rows until confirm. */
export async function bookingsImportPreview(token: string, file: File): Promise<BookingImportPreview> {
  const form = new FormData();
  form.append("file", file);
  const res = await fetch(`${portalBase()}/bookings/import/preview`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    const msg =
      (typeof data.message === "string" && data.message) ||
      (typeof data.title === "string" && data.title) ||
      "Import preview failed";
    throw new Error(msg);
  }
  return {
    sessionId: String(data.sessionId ?? ""),
    completedCount: Number(data.completedCount ?? 0),
    draftCount: Number(data.draftCount ?? 0),
    skippedEmptyRows: Number(data.skippedEmptyRows ?? 0),
    totalDataRows: Number(data.totalDataRows ?? 0),
  };
}

export async function bookingsImportConfirm(
  token: string,
  body: { sessionId: string; importCompleted: boolean; importDrafts: boolean },
): Promise<BookingImportResult> {
  const res = await fetch(`${portalBase()}/bookings/import/confirm`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      sessionId: body.sessionId,
      importCompleted: body.importCompleted,
      importDrafts: body.importDrafts,
    }),
  });
  const data = (await res.json().catch(() => ({}))) as Record<string, unknown>;
  if (!res.ok) {
    const msg =
      (typeof data.message === "string" && data.message) ||
      (typeof data.title === "string" && data.title) ||
      "Import failed";
    throw new Error(msg);
  }
  return parseImportResult(data);
}

/** One multi-page PDF for completed bookings (waybills). */
export async function bookingsWaybillsBulkDownload(token: string, bookingIds: string[]): Promise<void> {
  const res = await fetch(`${portalBase()}/bookings/waybills/bulk`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ bookingIds }),
  });
  if (!res.ok) {
    const data = (await res.json().catch(() => ({}))) as { message?: string };
    throw new Error(data.message ?? "Could not generate waybills PDF");
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "waybills.pdf";
  a.click();
  URL.revokeObjectURL(url);
}

/** Download completed bookings as CSV or Excel (same template as import). */
export async function bookingsExportDownload(token: string, format: "csv" | "xlsx"): Promise<void> {
  const res = await fetch(`${portalBase()}/bookings/export?format=${format}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    const data = (await res.json().catch(() => ({}))) as { message?: string };
    throw new Error(data.message ?? "Export failed");
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `bookings-export.${format === "csv" ? "csv" : "xlsx"}`;
  a.click();
  URL.revokeObjectURL(url);
}

// --- Dashboard stats (JWT required) ---

export type CountByKey = { key: string; count: number };

export type DashboardScope = "all" | "drafts";

export type SunburstNode = { name: string; value: number; children?: SunburstNode[] };

export type SankeyGraph = {
  nodes: { name: string }[];
  links: { source: string; target: string; value: number }[];
};

export type DailyCount = { date: string; count: number };

export type KpiExtended = {
  avgPerDayLast30: number;
  avgPerDayThisMonth: number;
  avgPerDayThisYear: number;
  possiblyStuckCount: number;
};

export type DeliveryTimeDistribution = {
  sampleSize: number;
  minHours: number;
  q1Hours: number;
  medianHours: number;
  q3Hours: number;
  maxHours: number;
  outlierCount: number;
  sampleHours: number[];
};

export type HeatmapCell = { dayOfWeek: number; hour: number; count: number };

export type HeatmapGrid = { cells: HeatmapCell[]; maxCount: number };

export type DashboardStats = {
  scope: string;
  countToday: number;
  countMonth: number;
  countYear: number;
  byCourier: CountByKey[];
  fromCities: CountByKey[];
  toCities: CountByKey[];
  carrierServiceSunburst: SunburstNode | null;
  laneSankey: SankeyGraph;
  bookingsPerDayLast30: DailyCount[];
  bookingsPerDayCurrentMonth: DailyCount[];
  kpi: KpiExtended;
  deliveryTime: DeliveryTimeDistribution;
  exceptionSignalsHeatmap: HeatmapGrid;
};

export async function getDashboardStats(
  token: string,
  scope?: DashboardScope | null,
): Promise<DashboardStats> {
  const q =
    scope && scope !== "all"
      ? `?scope=${encodeURIComponent(scope)}`
      : "";
  const res = await fetch(`${portalBase()}/dashboard/stats${q}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to load dashboard stats");
  return res.json();
}
