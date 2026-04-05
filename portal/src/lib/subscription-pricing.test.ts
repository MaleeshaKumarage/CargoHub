import { describe, expect, it, vi } from 'vitest';
import { buildSubscriptionPricingLines } from '@/lib/subscription-pricing';
import type { PortalCompanySubscriptionDto } from '@/lib/api';

function t(key: string, _values?: Record<string, string | number>) {
  return key;
}

describe('buildSubscriptionPricingLines', () => {
  it('returns trial line with count', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'Trial',
      planKind: 'Trial',
      currency: 'EUR',
      trialBookingAllowance: 5,
    };
    const spy = vi.fn(t);
    const lines = buildSubscriptionPricingLines(sub, spy);
    expect(lines).toHaveLength(1);
    expect(spy).toHaveBeenCalledWith('subscriptionTrialBookings', { count: 5 });
  });

  it('returns per-booking line for PayPerBooking', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'Paygo',
      planKind: 'PayPerBooking',
      currency: 'EUR',
      chargePerBooking: 9.99,
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines.length).toBe(1);
    expect(lines[0]).toContain('subscriptionPerBooking');
  });
});
