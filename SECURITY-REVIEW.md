# Evidence query security review

## Scope

Review of the supplied deployed query after migration
`20261119_AddExternalCustomerMapping`. The workspace
[reader](apps/fe-evidence/app.cs) already uses the join-free correction.
No live Azure resources or production database were inspected.

## Finding

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟡 MEDIUM | Supplied deployed query | 14-15 | Mapping join omits CompanyId, permitting cross-company associations and foreign-tenant influence on result cardinality | 10/10 |

Customer uniqueness is `(CompanyId, Id)`. Joining only
`customer.Id == mapping.CustomerId` does not isolate mappings to the customer's
company. Another company's mappings with the same customer GUID can affect
whether the query returns a result or throws.

Ticket disclosure is not established by this query: the customer root is scoped,
and a correctly configured composite ticket relationship can still protect the
ticket collection. The finding is not a claim of demonstrated ticket exfiltration.

## Availability defect

The inner join produces one result per matching mapping. One mapping previously
satisfied `SingleOrDefaultAsync`; multiple mappings violate its cardinality
assumption. Zero mappings also incorrectly exclude an existing customer.
Customer primary-key uniqueness does not prevent this join fan-out.

## Correction

Remove the unused mapping join. Select the customer by both company and customer
ID, then project only that company's ticket DTOs. Keep `SingleOrDefaultAsync` at
the customer level.

If mapping existence becomes a business requirement, use a company-scoped
`Any` predicate rather than a multiplicity-changing join. Mapping relationships
must reference customers using the composite `(CompanyId, CustomerId)` key.

## Executed verification

[SQLite/xUnit tests](apps/fe-evidence.Test/TicketReaderTests.cs) cover zero, one,
and two own-company mappings; foreign-company mappings sharing the same customer
GUID; multiple tickets; and cross-company denial.

- Corrected reader: all six cases passed without the Nx cache.
- Temporarily reconstructed deployed join: one case passed and five failed.
- One own mapping and no foreign mappings passed with the deployed join.
- Two own mappings reproduced `Sequence contains more than one element`.
- Foreign-company mapping collisions also reproduced the exception.
- Zero mappings incorrectly returned null.
- The reconstructed join and temporary EF mapping entity were removed; the
  corrected reader was restored and all six cases passed again.

These are reader/database tests, not end-to-end JWT authentication or HTTP
authorization tests. Before deployment, also run the migration and query suite
against the production database provider on a disposable restored database,
then canary affected customers and verify tenant isolation.
