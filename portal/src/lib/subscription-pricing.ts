import type { PortalCompanySubscriptionDto } from '@/lib/api';

function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount);
  } catch {
    return `${amount} ${currency}`;
  }
}

/**
 * Human-readable pricing lines for the More page (copy uses next-intl keys under `more.*`).
 */
export function buildSubscriptionPricingLines(
  sub: PortalCompanySubscriptionDto,
  t: (key: string, values?: Record<string, string | number>) => string,
): string[] {
  const c = sub.currency || 'EUR';
  const fmt = (n: number) => formatMoney(n, c);

  switch (sub.planKind) {
    case 'None':
      return [t('subscriptionNonePlan')];
    case 'Unknown':
      return [t('subscriptionUnknownPlan')];
    case 'Trial': {
      const n = sub.trialBookingAllowance;
      return [n != null ? t('subscriptionTrialBookings', { count: n }) : t('subscriptionTrial')];
    }
    case 'PayPerBooking':
      return sub.chargePerBooking != null
        ? [t('subscriptionPerBooking', { amount: fmt(sub.chargePerBooking) })]
        : [t('subscriptionRatesPending')];
    case 'MonthlyBundle': {
      const bits: string[] = [];
      if (sub.monthlyFee != null) bits.push(t('subscriptionMonthlyBase', { amount: fmt(sub.monthlyFee) }));
      if (sub.includedBookingsPerMonth != null)
        bits.push(t('subscriptionIncludedBookings', { count: sub.includedBookingsPerMonth }));
      if (sub.overageChargePerBooking != null && sub.includedBookingsPerMonth != null)
        bits.push(t('subscriptionOverageEach', { amount: fmt(sub.overageChargePerBooking) }));
      return bits.length > 0 ? bits : [t('subscriptionRatesPending')];
    }
    case 'TieredPayPerBooking':
      if (sub.tiers?.length) {
        return sub.tiers.map((tier) => {
          const max = tier.inclusiveMaxBookingsInPeriod;
          const band =
            max != null ? t('subscriptionTierBandMax', { max }) : t('subscriptionTierBandOpen');
          if (tier.chargePerBooking != null)
            return `${band}: ${fmt(tier.chargePerBooking)} ${t('subscriptionPerBookingShort')}`;
          return band;
        });
      }
      return [t('subscriptionRatesPending')];
    case 'TieredMonthlyByUsage':
      if (sub.tiers?.length) {
        return sub.tiers.map((tier) => {
          const max = tier.inclusiveMaxBookingsInPeriod;
          const band =
            max != null ? t('subscriptionTierBandMax', { max }) : t('subscriptionTierBandOpen');
          if (tier.monthlyFee != null)
            return `${band}: ${fmt(tier.monthlyFee)} ${t('subscriptionPerMonthShort')}`;
          return band;
        });
      }
      return [t('subscriptionRatesPending')];
    default:
      return [t('subscriptionRatesPending')];
  }
}
