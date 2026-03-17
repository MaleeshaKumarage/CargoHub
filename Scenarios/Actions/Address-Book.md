# Actions – Address book

## View address book

If the user is signed in and opens the Actions page, then the app fetches the address book for their company. The page shows the company name (if available), a section to add senders, a section to add receivers, and tables listing existing senders and receivers (name, city, etc.). If there are no senders or receivers, the tables show an empty message.

## Add sender

If the user fills in the sender form (name, address, postal code, city, country, and optionally email and phone) and clicks "Add sender", then the app sends the new sender to the server. If the server accepts, the address book is refreshed and the new sender appears in the senders table. The form is cleared. If the server returns an error (for example duplicate entry), the page shows the error message.

## Add receiver

If the user fills in the receiver form and clicks "Add receiver", then the app sends the new receiver to the server. If the server accepts, the address book is refreshed and the new receiver appears in the receivers table. The form is cleared. If the server returns an error, the page shows the error message.

## Duplicate entry

If the user tries to add a sender or receiver that already exists (same name, address, etc.), then the server or the app may detect the duplicate and show a message that the entry already exists. The user can change the data or skip adding.

## Load error (no company)

If the user has no company linked or the company is not found, then when the Actions page loads, the server returns an error. The page shows an error message (for example "Company not found. Your account may not be linked to a company."). The user cannot see or edit the address book.

## Super Admin – multiple companies

If the signed-in user is a Super Admin, then the app may fetch address books for all companies. The page may show a dropdown to select which company's address book to view. When the user adds a sender or receiver, the app sends the selected company ID so the entry is added to the correct address book.
