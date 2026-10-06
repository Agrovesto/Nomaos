# Nomaos microservices

The solution now contains four independently runnable API hosts. They retain the
existing Core, Infrastructure, and SQL Server schema during the first migration
phase, so existing models and API contracts remain compatible.

| Service | Project | Exposed API area |
| --- | --- | --- |
| Onboarding | `Agrovesto.Backend.Onboarding.API` | Authentication, organization, farmer, agent, membership, profile, verification, bulk upload, and farm mapping |
| Wallet | `Agrovesto.Backend.Wallet.API` | Wallet and bank APIs |
| Input financing | `Agrovesto.Backend.InputFinancing.API` | Input financing APIs |
| Monitoring and evaluation | `Agrovesto.Backend.MonitoringEvaluation.API` | M&E APIs |
| Warehouse | `Agrovesto.Backend.Warehouse.API` | Warehouse, inventory, warehouse-manager, and bundle APIs |
| Service provider | `Agrovesto.Backend.ServiceProvider.API` | Payment, wallet, and SMS provider configuration APIs |

Each host explicitly allowlists its controllers. Background jobs from the
monolith are intentionally not started by these hosts, preventing duplicate
scheduling while deployments overlap.

## Containers

`Dockerfile.microservice` builds any host using its project and assembly build
arguments. `docker-compose.microservices.yml` runs all six hosts on ports
5101 through 5106. It requires the shared transition database connection and
JWT settings to be supplied as environment variables; no secrets are stored in
the Compose file.

```powershell
$env:NOMAOS_ConnectionStrings__DefaultConnection = "<sql-server-connection-string>"
$env:NOMAOS_Jwt__Issuer = "https://your-issuer"
$env:NOMAOS_Jwt__Audience = "https://your-audience"
$env:NOMAOS_Jwt__Key = "<jwt-signing-key>"
docker compose -f docker-compose.microservices.yml up --build
```

## Data ownership

The `20261006141000_create_service_schemas` migration reserves these SQL Server
schemas. It does not move existing tables, so it is safe to apply while the
monolith and new hosts share the database.

| Schema | Owner | First tables to move |
| --- | --- | --- |
| `onboarding` | Onboarding | Identity users and roles, profile and verification data, organizations, agents, memberships, farmers, farms, locations, OTPs, revoked tokens, and bulk onboarding jobs |
| `wallet` | Wallet | User wallets, wallet contexts, wallet transactions, provider transactions, reconciliation logs, and bank-wallet configuration |
| `input_financing` | Input financing | Input packages, package items, commitment and repayment plans, subscriptions, disbursements, repayment schedules, financing schemes, EOPs, and financing invoices |
| `monitoring_evaluation` | Monitoring and evaluation | M&E datasets, fields, records, charts, activity types, sub-activities, field definitions, and farm activities |
| `warehouse` | Warehouse | Warehouses, warehouse inputs and inventory transactions, warehouse users, bundles, bundle items, and storage categories |
| `service_provider` | Service provider | Payment, wallet, and SMS providers with their parameter and business-specific configuration |

Tables that have foreign keys across these boundaries must be copied to their
new schema in dependency order. Replace each cross-domain foreign key with a
stable external identifier and a service API or event before moving the
dependent table.

## Integration events

Each host runs an SNS outbox dispatcher. Set `Outbox__TopicArn` (or
`NOMAOS_Outbox__TopicArn` in a host container) to enable publishing. Events are
written in the same database transaction as their domain change, then published
at least once with `eventType`, `eventId`, and `source` message attributes.
Consumers must deduplicate by `eventId`.

The Warehouse service emits `warehouse.created`. The Service Provider service
emits `provider.created`, `provider.enabled`, `provider.disabled`, and
`provider.configuration-changed`. Provider event payloads identify the provider
and optional business context, but never include credential or parameter values.

## Running a service

Configure each host through environment variables. The `NOMAOS_` prefix keeps
service configuration separate from the process environment:

```powershell
$env:NOMAOS_ConnectionStrings__DefaultConnection = "<sql-server-connection-string>"
$env:NOMAOS_Jwt__Issuer = "https://your-issuer"
$env:NOMAOS_Jwt__Audience = "https://your-audience"
$env:NOMAOS_Jwt__Key = "<jwt-signing-key>"
dotnet run --project Agrovesto.Backend.Wallet.API
```

Every host exposes `/health` and a root endpoint that reports its service name.
Use a separate deployment and environment configuration per service, with
least-privileged database credentials. The shared schema is transitional; move
each service to a service-owned schema or database before removing the
monolith.
