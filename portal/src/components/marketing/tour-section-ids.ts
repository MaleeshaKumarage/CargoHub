/** DOM ids for /tour anchor navigation (marketing page only). */
export const TOUR_SECTION_IDS = [
  "top",
  "platform",
  "bookings",
  "dashboard",
  "workflow",
  "integrations",
  "carriers",
  "admin",
  "subscriptions",
  "start",
] as const;

export type TourSectionId = (typeof TOUR_SECTION_IDS)[number];
