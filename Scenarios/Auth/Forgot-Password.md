# Forgot password

## Submit email

If the user enters their email and submits "Send reset link", then the app sends a request to the server. After the request is sent, the page shows a message that a reset link has been sent. The user can use "Back to sign in" to return to the login page.

## Invalid or unknown email

If the user enters an email that is not registered, the server may still accept the request (to avoid revealing whether the email exists). The page may show the same success message. No email is sent for unknown addresses.

## Network or server error

If the user enters an email and the server is unreachable or returns an error, the page may show the success message or an error, depending on implementation. The user can try again or go back to sign in.
