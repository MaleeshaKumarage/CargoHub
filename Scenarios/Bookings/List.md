# Bookings list

## View completed bookings

If the user is signed in and opens the bookings page, then the "Completed" tab is selected by default. The app fetches the list of completed bookings for the user's company. If the request succeeds, the page shows a table with reference/shipment number, customer, created date, milestone bar, and a "View" link for each booking. If there are no bookings, the page shows an empty message and a link to create a booking.

## Switch to drafts

If the user clicks the "Drafts" tab, then the app fetches the list of drafts. The page shows drafts in a table with an "Edit / Confirm" link for each. If there are no drafts, the page shows an empty message and a link to create a booking.

## List load error

If the user is signed in and the request to load the bookings or drafts list fails, then the page shows an error message. The user can refresh or try again.

## Open a booking

If the user clicks "View" on a completed booking row, then the app navigates to the booking detail page. If the user clicks "Edit / Confirm" on a draft row, then the app navigates to the draft detail page.

## Create booking button

If the user clicks the "Create booking" button, then the app navigates to the create-booking page.

## Super Admin

If the signed-in user is a Super Admin, then the list may show bookings from all companies. The list might include a company column or filter. Super Admin cannot create bookings; the create button may be hidden or disabled, or the create page may show an error when they try to submit.
