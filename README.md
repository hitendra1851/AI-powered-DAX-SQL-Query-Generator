# QueryMind AI — Enterprise DAX & SQL Query Generator

**AI-powered multi-tenant SaaS platform that generates DAX (Power BI / Analysis Services) and SQL queries from natural language.**

Built with .NET Core 8 + React 18 + Claude AI (claude-sonnet-4-20250514) + Azure.

---

## What It Does

- **Natural language → DAX or SQL**: ask "Show me YTD sales by region vs last year" and get production-ready code
- **Schema-aware**: upload your PBIX JSON, SQL DDL, CSV headers, Fabric Lakehouse schema, or Salesforce metadata — the AI uses your exact table/column/measure names
- **Streaming responses**: real-time SSE token streaming, no waiting for full response
- **Multi-tenant**: full tenant isolation, RBAC, audit logging, plan limits (Starter/Pro/Enterprise)
- **Query history & feedback**: every query is persisted; thumbs up/down feedback improves future results

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    React Frontend (SPA)                  │
│   Dashboard │ Schema Manager │ Query Studio │ History    │
└──────────────────────┬──────────────────────────────────┘
                       │ REST + SSE
┌──────────────────────▼──────────────────────────────────┐
│               .NET Core 8 Web API                        │
│   Controllers │ Middleware │ JWT Auth │ Rate Limiting    │
└──────┬──────────────────────────────┬───────────────────┘
       │ MediatR CQRS                 │ AI Agent
┌──────▼───────────────┐   ┌──────────▼──────────────────┐
│  Application Layer   │   │   QueryMind AI Layer         │
│  Commands │ Queries  │   │   Claude API + Tool Use      │
│  DTOs │ Handlers     │   │   SSE Streaming │ RAG        │
└──────┬───────────────┘   └──────────┬──────────────────┘
       │                              │
┌──────▼──────────────────────────────▼──────────────────┐
│              Infrastructure Layer                        │
│  EF Core (PostgreSQL) │ Azure Blob │ Azure AI Search    │
│  Schema Parsers (6 types) │ Audit Service               │
└─────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET Core 8 Web API (C#), CQRS + MediatR |
| Frontend | React 18 + TypeScript + Tailwind CSS |
| AI | Anthropic Claude API (`claude-sonnet-4-20250514`) with tool use |
| Auth | Azure Entra ID (MSAL) + API Key fallback |
| Storage | Azure Blob Storage (schema files, tenant-scoped) |
| Search | Azure AI Search (RAG over schema embeddings) |
| Database | PostgreSQL 16 + EF Core 8 |
| Deploy | Docker + Azure Container Apps |
| Monitoring | Serilog + OpenTelemetry |

---

## Quick Start (Docker)

**Prerequisites:** Docker, Anthropic API key

```bash
# Clone the repo
git clone https://github.com/hitendra1851/ai-powered-dax-sql-query-generator.git
cd ai-powered-dax-sql-query-generator

# Set your Anthropic API key
export ANTHROPIC_API_KEY=sk-ant-...

# Start all services
docker-compose up -d

# Frontend: http://localhost:3000
# API:      http://localhost:5000
# Swagger:  http://localhost:5000/swagger
```

---

## Local Development (without Docker)

### Backend

```bash
# Prerequisites: .NET 8 SDK, PostgreSQL running locally

# Set configuration (or use appsettings.Development.json)
export Anthropic__ApiKey="sk-ant-..."
export ConnectionStrings__Postgres="Host=localhost;Database=querymind;Username=querymind;Password=querymind_dev"

# Run API
cd src/QueryMind.API
dotnet run

# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# http://localhost:3000
```

---

## Configuration

Key settings in `appsettings.json` / environment variables:

| Key | Description |
|---|---|
| `Anthropic:ApiKey` | Anthropic API key (required) |
| `ConnectionStrings:Postgres` | PostgreSQL connection string |
| `Azure:StorageConnectionString` | Azure Blob Storage connection |
| `Azure:SearchEndpoint` | Azure AI Search endpoint URL |
| `Azure:SearchApiKey` | Azure AI Search API key |
| `Azure:Authority` | Azure Entra ID authority URL |
| `Azure:ClientId` | Azure Entra app client ID |

For local dev without Azure Search, the AI falls back to using the full parsed schema JSON as context.

---

## Schema Formats Supported

| Format | Extension | Use Case |
|---|---|---|
| PBIX JSON | `.json` | Power BI model.json from extracted PBIX |
| SQL DDL | `.sql`, `.ddl` | T-SQL, PostgreSQL, MySQL CREATE TABLE scripts |
| CSV Headers | `.csv` | Column name inference with type detection |
| Tabular BIM | `.json` (contains "bim") | Analysis Services BIM format |
| Fabric Lakehouse | `.json` (contains "fabric") | Microsoft Fabric Delta table schemas |
| Salesforce | `.json` (contains "salesforce") | Salesforce Describe API JSON exports |

---

## API Reference

### Authentication

Use either:
- **Azure Entra JWT**: `Authorization: Bearer <token>`
- **API Key**: `X-Api-Key: <your-key>`

### Key Endpoints

```
POST /api/schemas/upload          Upload schema file (multipart/form-data)
GET  /api/schemas                 List tenant schemas
DELETE /api/schemas/{id}          Delete schema (soft delete + cleanup)

POST /api/sessions                Create query session
POST /api/sessions/{id}/messages  Send message (SSE streaming response)
GET  /api/sessions/{id}/history   Full conversation history

POST /api/feedback                Rate a query (1-5 stars)
GET  /api/templates               Query template library
GET  /api/admin/usage             Token + query usage metrics
GET  /api/health                  Health check
```

Full Postman collection: `docs/postman_collection.json`

---

## Subscription Plans

| Plan | Price | Queries/mo | Schemas |
|---|---|---|---|
| Starter | $29/org | 500 | 3 |
| Pro | $99/org | 5,000 | Unlimited |
| Enterprise | $499/org | Unlimited | Unlimited |

Plan limits are enforced at the API layer with 429 responses including usage details.

---

## Deploying to Azure

```bash
# Prerequisites: Azure CLI, existing resource group

az deployment group create \
  --resource-group rg-querymind-prod \
  --template-file deploy/bicep/main.bicep \
  --parameters \
    appName=querymind \
    environment=prod \
    anthropicApiKey="$ANTHROPIC_API_KEY" \
    postgresAdminPassword="$POSTGRES_PASSWORD"
```

The Bicep template provisions:
- Azure Container Apps (API + Frontend)
- PostgreSQL Flexible Server
- Azure Blob Storage (tenant-scoped containers)
- Azure AI Search (schema RAG index)

---

## Running Tests

```bash
# .NET unit tests
dotnet test tests/QueryMind.UnitTests

# .NET integration tests (requires local DB)
dotnet test tests/QueryMind.IntegrationTests

# Playwright E2E (requires frontend running on :3000)
cd tests/QueryMind.E2ETests
npx playwright install
npx playwright test
```

---

## Sample Schemas

In `samples/schemas/`:
- `powerbi_model_sample.json` — Sales star schema (Power BI PBIX JSON format)
- `healthcare_star_schema.sql` — Wound care analytics SQL DDL (SNF/HHA billing)
- `fabric_lakehouse_schema.json` — Bronze/Silver/Gold Fabric Lakehouse schema

---

## Example Queries You Can Generate

**DAX:**
- "Show me YTD sales vs same period last year by product category"
- "What is the top 10 products by revenue this quarter?"
- "Calculate 3-month rolling average of gross margin by region"

**SQL (T-SQL / Spark SQL):**
- "Show healing rate trend by wound type over the last 6 months"
- "Which facilities have the highest denial rates for Medicare claims?"
- "Find patients with more than 3 wound types and their total cost"

**Fabric Lakehouse (Spark SQL):**
- "Query the Gold layer for monthly wound analytics by facility type"
- "Join Bronze and Silver layers to find ingestion errors from last week"

---

## Security & Compliance

- **Tenant isolation**: all data filtered by `TenantId` at DB + storage layers
- **HIPAA-ready**: full audit logging on all AI interactions (entity, action, user, IP, timestamp)
- **Schema security**: files stored in tenant-scoped Azure Blob containers; no cross-tenant access
- **No data retention**: schema data never sent to Anthropic training; API calls are inference-only
- **GDPR**: right-to-delete wipes schemas + query history + audit logs for a tenant
- **API key security**: SHA-256 hashed storage; plaintext never persisted

---

## Project Structure

```
querymind/
  src/
    QueryMind.Domain/          Entities, enums, domain interfaces
    QueryMind.Infrastructure/  EF Core, Azure SDKs, 6 schema parsers
    QueryMind.Application/     CQRS commands/queries, MediatR handlers
    QueryMind.AI/              Claude agent, tool use, SSE streaming
    QueryMind.API/             Controllers, middleware, Program.cs
  frontend/                   React 18 + TypeScript SPA
  tests/
    QueryMind.UnitTests/       Parser + domain logic tests
    QueryMind.IntegrationTests/ API integration tests
    QueryMind.E2ETests/        Playwright browser tests
  deploy/
    docker/                   Dockerfiles + nginx config
    bicep/                    Azure IaC templates
  samples/schemas/            Sample PBIX, SQL DDL, Fabric JSON
  docs/                       Postman collection
  docker-compose.yml
```

---

## Built By

Hiten Patel — Microsoft Fabric & Power BI analytics specialist, healthcare data engineer.

Domain expertise: Vohra wound care analytics, Fabric Lakehouse Gold-layer DAX, Salesforce SOQL.
