# Booking detail

## View completed booking

If the user is signed in and opens the detail page for a completed booking, the app fetches the booking. The page shows header, milestone bar, shipper and receiver, shipment info, packages, and a Print waybill button. The user cannot edit. There is no Edit button for completed bookings.

## Print waybill

If the user clicks Print waybill, the app requests the waybill PDF. If the request succeeds, the app opens the PDF in a new tab. The user can print or save it. If the server returns an error, the page shows an error message.

## Booking not found

If the user opens a detail page for a booking ID that does not exist, the page shows Booking not found.

## After confirm

If the user has just confirmed a draft and the app redirects here with a print waybill parameter, the page may automatically open the waybill PDF after the booking loads.
