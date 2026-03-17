# Manage – Companies (Super Admin)

## View companies list

If the signed-in user is a Super Admin and opens Manage then Companies (or the companies page), then the app fetches the list of companies. The page shows companies with name, business ID, company ID, and possibly other details. The Super Admin can see all companies in the system.

## Create company

If the Super Admin fills in the form to create a new company (name, business ID, etc.) and submits, then the app sends the data to the server. If the server accepts, the new company is created and the list is updated. If the server returns an error (e.g. duplicate business ID), the page shows the error message.

## Not Super Admin

If the signed-in user is not a Super Admin and tries to open the manage companies page, then the app may redirect them or show an error. Only Super Admins can manage companies.
