# Create booking

## Successful create

If the user is signed in, has a company, fills in the required fields including receiver and courier, and clicks create (not Save as draft), then the server creates the booking and the app redirects to the booking detail page, optionally with print waybill.

## Save as draft

If the user fills in part of the form and clicks Save as draft, then the server saves the draft. The app redirects to the draft detail page. The user can later complete and confirm the draft.

## Validation errors

If the user leaves required fields empty or enters invalid data and submits, the server returns validation errors. The page shows the error message. The user stays on the page and can correct the fields.

## No company

If the user has no company, then when they try to create or save a draft the server may return an error. The page shows the message. The user may be directed to contact an administrator.

## Address book

If the company has senders and receivers in the address book, the create page may show dropdowns to pick one. Selecting one fills the form. If the user checks Save to address book, after creating the booking or draft the app may add the shipper and receiver to the address book.

## Network or server error

If the user submits and the server is unreachable or returns a generic error, the page shows an error message. The user can try again.
