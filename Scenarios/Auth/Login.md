# Login

## Successful sign-in

If the user enters a valid email (or account) and the correct password and the server accepts them, then the user is signed in, the app stores the session, and the user is taken to the dashboard.

## Wrong password

If the user enters a valid account but the wrong password, then the server returns an error. The page shows an error message (e.g. "Invalid credentials" or the message from the API). The user stays on the login page and can try again.

## Account not found or invalid

If the user enters an account that does not exist or is not valid and submits, then the server may return an error. The page shows the error message. The user stays on the login page.

## Empty fields

If the user leaves the account or password field empty and tries to submit, then browser validation (required fields) prevents submit, or the request fails. The user must fill in the fields.

## Network or server error

If the user enters any account and password but the server is unreachable or returns a generic error, then the page shows a network or generic error message. The user stays on the login page and can try again.

## Already signed in

If the user is already signed in and opens the login page, then the app redirects them to the dashboard. They do not see the login form.
