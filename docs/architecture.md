# Arquitetura

## Objetivo

O sistema registra lançamentos financeiros de crédito/débito e consolida saldos diários sem bloquear a aplicação de gestão quando a consolidação estiver indisponível.

## C4 - Container

```mermaid
flowchart LR
  user["Usuário"] --> web["React Web"]
  web --> api["Rox.FinancialControl.Api"]
  api --> db[("SQL Server")]
  api --> outbox["Outbox Publisher"]
  outbox --> rabbit[("RabbitMQ")]
  rabbit --> worker["Rox.FinancialControl.Worker"]
  worker --> db
```

## Camadas do backend

```mermaid
flowchart TB
  api["Api: endpoints, middleware, OpenAPI"] --> app["Application: use cases, DTOs, interfaces"]
  worker["Worker: message consumer host"] --> infra["Infrastructure: EF Core, MassTransit, repositories"]
  infra --> app
  app --> domain["Domain: CashEntry, DailyBalance, invariants"]
  infra --> domain
```

## Fluxo de criação de lançamento

```mermaid
sequenceDiagram
  participant Web
  participant Api
  participant Sql as SQL Server
  participant Pub as Outbox Publisher
  participant Rabbit as RabbitMQ
  participant Worker

  Web->>Api: POST /api/cash-entries
  Api->>Sql: insert cash_entries + outbox_messages
  Api-->>Web: 201 Created
  Pub->>Sql: read pending outbox
  Pub->>Rabbit: publish CashEntryRegisteredIntegrationEvent
  Pub->>Sql: mark outbox processed
  Rabbit->>Worker: deliver message
  Worker->>Sql: upsert daily_balances + processed_cash_entries
```

## Resiliência

- Se o worker parar, a API continua salvando lançamentos.
- Se uma mensagem for entregue duas vezes, `processed_cash_entries` impede dupla consolidação.
- Se a publicação falhar, a outbox fica pendente e será tentada novamente.
- O consumidor usa retry exponencial para falhas transitórias.
- A fila desacopla a taxa de entrada da taxa de processamento da consolidação.

## Modelo de dados

```mermaid
erDiagram
  CASH_ENTRIES {
    uniqueidentifier Id PK
    date BusinessDate
    string Type
    decimal Amount
    string Description
    datetimeoffset OccurredAt
    datetimeoffset RegisteredAt
  }

  DAILY_BALANCES {
    date BusinessDate PK
    decimal TotalCredits
    decimal TotalDebits
    int EntriesCount
    datetimeoffset LastUpdatedAt
  }

  OUTBOX_MESSAGES {
    uniqueidentifier Id PK
    string Type
    string Payload
    datetimeoffset OccurredAt
    datetimeoffset ProcessedAt
    int Attempts
    string Error
  }

  PROCESSED_CASH_ENTRIES {
    uniqueidentifier CashEntryId PK
    datetimeoffset ProcessedAt
  }
```
