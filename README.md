# Service for basic banking operations

This project implements a simple "in-memory banking service" that simulates basic banking operations such as account creation, deposits, withdrawals, and transfers.

The goal of this exercise is not to build a full application or API, but to model a "banking domain component" with attention to real-world constraints like consistency, atomicity, and concurrency.

---

## Scope & Design Goals

- Focus on "domain logic", not infrastructure
- In-memory storage (no database, no persistence)
- Thread-safe operations under concurrent access
- Clear and predictable behavior in failure scenarios

This service is designed as a "software module", intended to be used as a building block in a larger system.

---

## Supported Operations

- Create account with initial deposit
- Deposit funds
- Withdraw funds (no overdrafts allowed)
- Transfer funds between accounts
- Query account balance

All operations validate account existence and enforce domain invariants.

---

## Concurrency & Consistency

- Account state is stored in a `ConcurrentDictionary`
- Each account uses "per-account locking" to avoid race conditions
- Transfers lock both source and destination accounts
- A "deterministic lock ordering" (based on account IDs) is used to prevent deadlocks
- Transfers are atomic: debit and credit either both succeed or neither does

The system behaves as if operations were executed sequentially, even under heavy concurrency.

---

## Domain Invariants

- Account balances never become negative
- Transfers are all-or-nothing
- Total balance across accounts remains consistent under concurrent transfers
- Monetary amounts are validated via a dedicated `Money` type

---

## Testing

The solution includes:
- Unit tests covering all supported operations
- Failure-path tests (e.g. insufficient funds, unknown accounts)
- Concurrency tests that validate:
  - absence of race conditions
  - absence of deadlocks
  - preservation of balance invariants under parallel execution

---

## Out of Scope

The following concerns are intentionally not implemented:
- Persistence / durability (database, transaction logs)
- APIs or transport layers
- Idempotency and retry handling
- Multi-currency 

These would be addressed at higher architectural layers in a production system.

