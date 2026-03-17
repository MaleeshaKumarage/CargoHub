import { defineRouting } from 'next-intl/routing';

export const routing = defineRouting({
  locales: ['en', 'fi', 'sv', 'no', 'da', 'is'],
  defaultLocale: 'en',
  localePrefix: 'always',
});

export type Locale = (typeof routing.locales)[number];

export const localeNames: Record<Locale, string> = {
  en: 'English',
  fi: 'Suomi',
  sv: 'Svenska',
  no: 'Norsk',
  da: 'Dansk',
  is: 'Íslenska',
};
