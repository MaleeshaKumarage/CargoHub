import { describe, expect, it, vi } from 'vitest';
import { buildSubscriptionPricingLines } from '@/lib/subscription-pricing';
import type { PortalCompanySubscriptionDto } from '@/lib/api';

function t(key: string, values?: Record<string, string | number>) {
  void values;
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

  it('returns trial without count when allowance missing', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'Trial',
      planKind: 'Trial',
      currency: 'EUR',
    };
    const spy = vi.fn(t);
    buildSubscriptionPricingLines(sub, spy);
    expect(spy).toHaveBeenCalledWith('subscriptionTrial');
  });

  it('returns pending when PayPerBooking has no charge', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'P',
      planKind: 'PayPerBooking',
      currency: 'EUR',
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines).toEqual(['subscriptionRatesPending']);
  });

  it('builds MonthlyBundle lines when fees set', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'MB',
      planKind: 'MonthlyBundle',
      currency: 'EUR',
      monthlyFee: 50,
      includedBookingsPerMonth: 100,
      overageChargePerBooking: 2,
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines.length).toBeGreaterThanOrEqual(3);
  });

  it('returns pending when MonthlyBundle has no fee fields', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'MB',
      planKind: 'MonthlyBundle',
      currency: 'EUR',
    };
    expect(buildSubscriptionPricingLines(sub, t)).toEqual(['subscriptionRatesPending']);
  });

  it('maps TieredPayPerBooking tiers', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'T',
      planKind: 'TieredPayPerBooking',
      currency: 'EUR',
      tiers: [
        { ordinal: 1, inclusiveMaxBookingsInPeriod: 10, chargePerBooking: 1, monthlyFee: null },
        { ordinal: 2, inclusiveMaxBookingsInPeriod: null, chargePerBooking: 0.5, monthlyFee: null },
      ],
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines).toHaveLength(2);
    expect(lines[0]).toContain('subscriptionPerBookingShort');
  });

  it('returns band only when tier charge missing', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'T',
      planKind: 'TieredPayPerBooking',
      currency: 'EUR',
      tiers: [{ ordinal: 1, inclusiveMaxBookingsInPeriod: 5, chargePerBooking: null, monthlyFee: null }],
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines[0]).toBe('subscriptionTierBandMax');
  });

  it('returns pending when TieredPayPerBooking has no tiers', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'T',
      planKind: 'TieredPayPerBooking',
      currency: 'EUR',
    };
    expect(buildSubscriptionPricingLines(sub, t)).toEqual(['subscriptionRatesPending']);
  });

  it('maps TieredMonthlyByUsage tiers', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'TM',
      planKind: 'TieredMonthlyByUsage',
      currency: 'EUR',
      tiers: [{ ordinal: 1, inclusiveMaxBookingsInPeriod: 3, chargePerBooking: null, monthlyFee: 20 }],
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines[0]).toContain('subscriptionPerMonthShort');
  });

  it('uses format fallback for invalid currency code', () => {
    const sub: PortalCompanySubscriptionDto = {
      planName: 'P',
      planKind: 'PayPerBooking',
      currency: 'NOT_A_CURRENCY_CODE_XX',
      chargePerBooking: 1,
    };
    const lines = buildSubscriptionPricingLines(sub, t);
    expect(lines[0]).toContain('subscriptionPerBooking');
  });
});
