# Reset password

## Valid token and new password

If the user opens the reset link (with a valid token) and enters a new password that meets the rules and submits, then the server updates the password. The user is told the password was reset and can sign in on the login page with the new password.

## Invalid or expired token

If the user opens a reset link with an invalid or expired token and enters a new password and submits, then the server returns an error. The page shows an error message. The user cannot reset with that link and may need to request a new reset link.

## Password does not meet rules

If the user enters a new password that is too short or does not meet server rules and submits, then the server returns a validation error. The page shows the error. The user stays on the page and can enter a valid password.

## Network or server error

If the user enters a valid new password but the server is unreachable or returns a generic error, the page shows an error. The user can try again.
