# ROX Financial Control

Desafio técnico para um sistema de fluxo de caixa com cadastro de créditos/débitos e consolidação diária assíncrona.

## Stack

- Backend: .NET 10, ASP.NET Core Minimal APIs, EF Core, SQL Server
- Mensageria: RabbitMQ + MassTransit
- Resiliência: outbox persistente + consumidor idempotente
- Frontend: React, TypeScript, Vite, TanStack Query, React Hook Form, Zod
- Testes: xUnit
- Containers: Docker Compose com API, Worker, Web, SQL Server e RabbitMQ

## Arquitetura

```text
React Web
  -> Rox.FinancialControl.Api
    -> SQL Server: cash_entries + outbox_messages
    -> OutboxPublisherBackgroundService
      -> RabbitMQ
        -> Rox.FinancialControl.Worker
          -> SQL Server: daily_balances + processed_cash_entries
```

A API salva o lançamento e a mensagem de outbox na mesma transação lógica do EF Core. Se o worker de consolidação falhar, a aplicação de gestão continua gravando lançamentos. Quando a fila/worker voltar, o publisher envia as mensagens pendentes e o worker atualiza o saldo diário. O worker registra cada `CashEntryId` processado para evitar duplicidade em caso de retry.

Os lançamentos são segregados pela origem:

- `Business`: lançamentos reais criados pela tela principal e usados nos relatórios operacionais.
- `Validation`: lançamentos gerados pela rotina automatizada de validação funcional.
- `LoadSimulation`: lançamentos gerados pela simulação de carga.

Por padrão, consultas de lançamentos, saldos e relatórios usam apenas `Business`. Assim, testes automatizados e simulações não contaminam o saldo real do fluxo de caixa.

Na tela `Lançamentos`, o filtro `Origem` permite alternar entre dados reais, validação funcional e teste de carga. O saldo consolidado, a tabela e o relatório baixado acompanham a origem selecionada.

Mais detalhes e diagramas: [docs/architecture.md](docs/architecture.md).

## Pré-requisitos

- .NET SDK 10
- Node.js 24+
- Docker Desktop, se quiser rodar o ambiente completo por containers

Nesta máquina, o .NET e o Node já foram encontrados. O Docker não estava disponível no PATH no momento da criação do projeto.

## Rodando com Docker

Depois de instalar/configurar o Docker Desktop:

```powershell
docker compose up --build
```

Opcionalmente, copie `.env.example` para `.env` para sobrescrever as credenciais locais usadas pelo Compose.

Serviços:

- Web: http://localhost:5173
- API OpenAPI: http://localhost:5080/openapi/v1.json
- Health: http://localhost:5080/health
- RabbitMQ Management: http://localhost:15672
- SQL Server: localhost,1433

Credenciais locais:

- SQL Server user: `sa`
- SQL Server password: `Your_strong_password123`
- RabbitMQ user/password: `guest` / `guest`

No Docker, o frontend chama a API por `/api` e o Nginx do container `web` faz proxy interno para `api:8080`. Isso evita problemas de CORS ou de diferença entre `localhost` e `127.0.0.1` no navegador.

### Validação funcional automatizada

Ao subir pelo Docker Compose, o `worker` também executa uma rotina agendada de validação funcional. Por padrão, ela roda uma vez após a inicialização e depois a cada 5 minutos, chamando a API para criar lançamentos controlados, validar regras de negócio, aguardar a consolidação assíncrona e conferir o saldo final.

Esses lançamentos são gravados com origem `Validation`. Eles passam pela mesma API, outbox, RabbitMQ, worker e SQL Server, mas são consolidados em saldos separados dos lançamentos reais.

Principais parâmetros no `.env`:

```env
VALIDATION_SCHEDULER_ENABLED=true
VALIDATION_SCHEDULER_MODE=Interval
VALIDATION_SCHEDULER_INTERVAL_MINUTES=5
VALIDATION_SCHEDULER_RUN_ON_STARTUP=true
VALIDATION_SCHEDULER_ENTRIES_COUNT=8
VALIDATION_SCHEDULER_CREDIT_PERCENTAGE=50
VALIDATION_SCHEDULER_CREDIT_AMOUNT=100
VALIDATION_SCHEDULER_DEBIT_AMOUNT=40
VALIDATION_SCHEDULER_TIMEOUT_SECONDS=30
```

Para rodar a cada 10 minutos:

```env
VALIDATION_SCHEDULER_MODE=Interval
VALIDATION_SCHEDULER_INTERVAL_MINUTES=10
```

Para rodar uma vez ao dia:

```env
VALIDATION_SCHEDULER_MODE=Daily
VALIDATION_SCHEDULER_DAILY_AT=02:00
VALIDATION_SCHEDULER_TIME_ZONE=America/Sao_Paulo
```

Os resultados ficam nos logs do worker:

```powershell
docker compose logs -f worker
```

### RabbitMQ Management

Abra explicitamente com HTTP:

```text
http://localhost:15672
```

Login:

```text
guest
guest
```

Se a tela ficar carregando indefinidamente, tente `http://127.0.0.1:15672`, faça um hard refresh (`Ctrl+F5`) ou abra em uma janela anônima. Pelo terminal, a API do management pode ser testada com:

```powershell
curl.exe -u guest:guest http://localhost:15672/api/overview
```

### Conectando pelo SQL Server Management Studio

No SSMS, use vírgula para informar a porta:

- Tipo de servidor: `Mecanismo de Banco de Dados`
- Nome do servidor: `localhost,1433`
- Autenticação: `Autenticação do SQL Server`
- Logon: `sa`
- Senha: `Your_strong_password123`

Se o SSMS pedir configuração de criptografia/certificado, abra `Opções >>` e marque `Confiar no certificado do servidor` ou selecione criptografia opcional.

## Rodando localmente sem Docker

Você precisa ter SQL Server e RabbitMQ rodando localmente com as configurações de `appsettings.Development.json`.

Backend:

```powershell
cd src/backend
dotnet restore Rox.FinancialControl.slnx
dotnet build Rox.FinancialControl.slnx
dotnet run --project Rox.FinancialControl.Api/Rox.FinancialControl.Api.csproj
dotnet run --project Rox.FinancialControl.Worker/Rox.FinancialControl.Worker.csproj
```

Frontend:

```powershell
cd src/frontend/rox-financial-control-web
npm install
npm run dev
```

## Testes

```powershell
cd src/backend
dotnet test Rox.FinancialControl.slnx
```

A suíte automatizada usa xUnit e cobre regras de domínio, casos de uso de aplicação e consultas de repositório com EF Core InMemory.

A interface também possui a aba `Validação funcional`, que executa uma rotina parametrizável de validação ponta a ponta: criação controlada de lançamentos, verificação das regras inválidas selecionadas, publicação via outbox e conferência do saldo consolidado pelo worker.

## Endpoints principais

Criar lançamento:

```http
POST /api/cash-entries
Content-Type: application/json

{
  "businessDate": "2026-08-14",
  "type": "Credit",
  "amount": 120.50,
  "description": "Venda local",
  "occurredAt": null,
  "origin": "Business"
}
```

Listar lançamentos:

```http
GET /api/cash-entries?from=2026-08-14&to=2026-08-14&page=1&pageSize=20&origin=Business
```

Listar saldos consolidados:

```http
GET /api/daily-balances?from=2026-08-14&to=2026-08-14&origin=Business
```

Status da outbox:

```http
GET /api/operations/outbox
```

Status da simulação de carga:

```http
GET /api/operations/load-simulation
```

Iniciar simulação de carga:

```http
POST /api/operations/load-simulation/start
Content-Type: application/json

{
  "requestsPerBatch": 20,
  "intervalSeconds": 60,
  "maxBatches": 10,
  "creditPercentage": 70,
  "minAmount": 20,
  "maxAmount": 600,
  "businessDate": "2026-08-15"
}
```

Parar simulação de carga:

```http
POST /api/operations/load-simulation/stop
```

Executar validação funcional automatizada:

```http
POST /api/operations/business-validation/run
Content-Type: application/json

{
  "entriesCount": 8,
  "creditPercentage": 50,
  "creditAmount": 100,
  "debitAmount": 40,
  "businessDate": "2026-08-15",
  "timeoutSeconds": 30,
  "includeInvalidCases": true
}
```

## Decisões técnicas

- Clean Architecture pragmática: Domain não depende de Application, Infrastructure ou API.
- Casos de uso explícitos no Application, sem acoplar regras a controllers/endpoints.
- EF Core encapsulado em repositories e unit of work.
- Outbox evita perda de lançamentos quando a consolidação ou RabbitMQ falham.
- Worker usa retries exponenciais, retry transitório do SQL Server e idempotência por `processed_cash_entries`.
- A consolidação diária usa lock transacional por data para evitar deadlocks sob mensagens simultâneas do mesmo dia.
- Saldos diários são segregados por origem (`Business`, `Validation`, `LoadSimulation`) para impedir que validações ou testes de carga alterem relatórios reais.
- Frontend separado por áreas funcionais e com cache controlado por TanStack Query.
- `EnsureCreatedAsync` foi usado para simplificar a execução local do desafio. Em produção, o passo natural seria trocar por migrations versionadas.

## Melhorias futuras

- Autenticação e autorização por perfil.
- Migrations EF versionadas e pipeline CI.
- Testes de integração com Testcontainers.
- Observabilidade com OpenTelemetry, tracing e dashboards.
- Dead-letter queue monitorada com tela operacional dedicada.
- Rate limiting na API e métricas de throughput da consolidação.
