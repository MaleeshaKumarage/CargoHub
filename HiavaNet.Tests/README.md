# HiavaNet.Tests

Unit and integration tests for the HiavaNet backend, aligned with:

- **[docs/Scope-Booking-Forms-And-Relationships.md](../docs/Scope-Booking-Forms-And-Relationships.md)** – Roles, editability, edge cases, API error contract, data model.

## Test database (same DB setup as app)

Tests use an **in-memory database** by default so you can run them without PostgreSQL. The same `ApplicationDbContext` and schema model as the real app are used, so behaviour matches one logical “test db” per run.

- **In-memory**: Each test class can use a unique database name so tests do not share data. No setup required; just run `dotnet test`.
- **Optional – real PostgreSQL test DB on the same server**: Create a second database (e.g. `portal_test`) on the same server, run migrations against it, set `ConnectionStrings__TestDb` to its connection string, and (if you add support in `TestDbFixture`) use it for integration tests. This keeps dev and test databases on the same server.

## What is tested

| Area | Tests | Doc reference |
|------|--------|----------------|
| **Booking.CompanyId** | `BookingCompanyIdTests` – Create booking/draft sets `CompanyId`; edge: minimal request. | Scope §7 |
| **API error codes** | `ApiErrorCodesTests` – Expected `errorCode` set, PascalCase, no duplicates, key codes present. | Scope §5 |
| **Integration (test DB)** | `Integration/BookingRepositoryTestDbTests` – Add booking/draft with CompanyId, read back, list by customer, get by id when missing. | Scope §7 |

Every test has a short English comment describing what is being tested and the expected outcome.

## Running tests

```bash
dotnet test HiavaNet.Tests
```

From solution root:

```bash
dotnet test --project HiavaNet.Tests
```

## Adding tests

1. Add unit tests for handlers/domain (mock `IBookingRepository`, `ICompanyRepository` as needed).
2. Add edge and negative cases with a simple English comment above each test.
3. For integration tests that need the test DB, use `TestDbFixture` to create a context and real repositories.
4. Keep `ApiErrorCodesTests.ExpectedErrorCodes` in sync when new error codes are introduced.
