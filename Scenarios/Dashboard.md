# Dashboard

## Signed-in user opens dashboard

If the user is signed in and opens the dashboard, then the page loads and shows a greeting (e.g. with their name). The app fetches dashboard statistics (bookings today, month, year, by courier, from/to cities). If the stats load successfully, the page shows summary cards and charts. The page also shows links to bookings, create booking, actions, plugin, and more.

## Stats load failure

If the user is signed in and the request for dashboard statistics fails (network or server error), then the page shows an error message where the stats would be. The rest of the page (greeting, links) still appears. The user can refresh or try again later.

## Not signed in

If the user is not signed in and tries to open the dashboard, then the app redirects them to the login page.
