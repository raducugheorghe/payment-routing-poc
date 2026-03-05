# PaymentRoutingPoc

A small .NET PoC that processes card payments through an application command flow and demonstrates payment routing/fallback behavior.

## Prerequisites

- .NET SDK `10.0.x` (the project targets `net10.0`)

## Run

### Option 1: Direct CLI

Bash:

```bash
dotnet run --project PaymentRoutingPoc.Api/PaymentRoutingPoc.Api.csproj
```

PowerShell:

```powershell
dotnet run --project PaymentRoutingPoc.Api/PaymentRoutingPoc.Api.csproj
```

API base URL (from launch settings): `http://localhost:5156`

### Option 2: Docker Compose

Bash:

```bash
docker-compose up
```

PowerShell:

```powershell
docker-compose up
```

API base URL (from launch settings): `http://localhost:11080`

## Call the API

Flow logs will be printed in the console where the API is running. You can use either of the following options to call the API and trigger the payment processing flow.
Calling the API multiple times will demonstrate the routing/fallback behavior as the simulated payment gateway randomly fails some requests.

> **Note:** If running the app using Docker Compose, update the port in the API call scripts from `5156` to `11080`.




### Option 1: JetBrains HTTP client (`.http`)

Use the request file at `docs/PaymentRoutingPoc.Api.http`.

Bash:

```bash
ijhttp docs/PaymentRoutingPoc.Api.http
```

PowerShell:

```powershell
ijhttp docs/PaymentRoutingPoc.Api.http
```

### Option 2: HTTP request

Bash:

```bash
curl -X POST http://localhost:5156/api/payments \
  -H "Accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{"amount":100.00,"currency":"USD","paymentMethod":"CreditCard","cardNumber":"4111111111111111","merchantId":"FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"}'
```

PowerShell:

```powershell
$body = @{
  amount = 100.00
  currency = "USD"
  paymentMethod = "CreditCard"
  cardNumber = "4111111111111111"
  merchantId = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5156/api/payments" `
  -ContentType "application/json" `
  -Headers @{ Accept = "application/json" } `
  -Body $body
```
