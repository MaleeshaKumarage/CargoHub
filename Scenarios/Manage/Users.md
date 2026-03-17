# Manage – Users (Super Admin)

## View users list

If the signed-in user is a Super Admin and opens Manage then Users (or the manage users page), then the app fetches the list of users. The page may allow filtering by company (business ID). The page shows users with email, display name, company, roles, and active status. The Super Admin can see users across all companies.

## Change user role or active status

If the Super Admin selects a user and changes their role (for example from User to Admin) or toggles active/inactive and saves, then the app sends the update to the server. If the server accepts, the user list is updated. If the server returns an error, the page shows the error message.

## Not Super Admin

If the signed-in user is not a Super Admin (for example they are Admin or User) and tries to open the manage users page, then the app may redirect them or show an error (for example 403). Only Super Admins can manage users globally.
