# Register

## Successful registration

If the user enters a valid company ID (business ID), email, user name, and password and the company exists in the system, then the server creates the account. The user is signed in and taken to the dashboard.

## Company ID required

If the user leaves the company ID empty or does not provide it and submits, then the server returns an error that the company ID is required. The page shows the corresponding message. The user stays on the register page.

## Company not found

If the user enters a company ID that does not exist (e.g. wrong number or company not set up by an administrator) and submits, then the server returns an error that the company was not found. The page shows the message. The user can correct the company ID or contact support.

## Invalid or duplicate email

If the user enters an email that is already registered or not valid and submits, then the server may return an error. The page shows the error message. The user stays on the register page.

## Network or server error

If the user fills the form correctly but the server is unreachable or returns a generic error, then the page shows a network or generic error. The user can try again.

## Already signed in

If the user is already signed in and opens the register page, then the app redirects them to the dashboard. They do not see the register form.
