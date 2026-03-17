# Draft list and draft edit

## View draft list

If the user is signed in and opens the bookings page and clicks the Drafts tab, the app fetches the list of drafts. The page shows each draft with an Edit or Confirm link. If there are no drafts, the page shows an empty message and a link to create a booking.

## Open draft to edit

If the user clicks Edit or Confirm on a draft, the app goes to the draft detail page and fetches the draft. The page shows the draft in a form with Save and Confirm buttons.

## Save draft changes

If the user edits fields and clicks Save, the app sends the updates to the server. If the save succeeds, the draft is updated. If the server returns an error, the page shows the error message.

## Confirm draft

If the user clicks Confirm and the server accepts, the draft becomes a completed booking. The app redirects to the completed booking detail page, often with a prompt to print the waybill. If the server returns an error, the page shows the error.

## Draft not found

If the user opens a draft for an ID that does not exist or was already confirmed, the page shows Draft not found.
