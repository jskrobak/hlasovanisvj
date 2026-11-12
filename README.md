# Hlasování SVJ

Blazor Server aplikace (.NET 10) pro hlasování na shromáždění SVJ: import vlastníků z KN,
QR lístky, docházka, plné moci (i pro hosty – nečleny s nulovým podílem), živé výsledky
a historie schůzí.

## Databáze

PostgreSQL přes EF Core (Npgsql). Migrace se aplikují automaticky při startu aplikace.

Connection string `ConnectionStrings:DefaultConnection` se v repu neukládá:

- **lokálně** přes user-secrets:
  ```bash
  dotnet user-secrets --project hlasovanisvj set "ConnectionStrings:DefaultConnection" "Host=...;Port=5435;Database=hlasovanisvj;Username=hlasovanisvj;Password=..."
  ```
- **produkce** přes proměnnou prostředí `ConnectionStrings__DefaultConnection`.

Vývojová databáze: [docker-compose.dev.yml](docker-compose.dev.yml)
(`PG_PASSWORD=... docker compose -f docker-compose.dev.yml up -d`, port 5435).

Nová migrace:
```bash
dotnet ef migrations add <Nazev> --project hlasovanisvj
```

## Konfigurace

- `BaseUrl` – veřejná adresa aplikace, použije se v QR kódech na hlasovacích lístcích.

## Schůze

- Účastníci, usnesení i hlasy patří vždy ke konkrétní schůzi; import nové schůze starší schůze neovlivní.
- Uzavřená schůze je jen pro čtení: výsledky usnesení (zafixované při uzavření hlasování) lze prohlížet,
  ale nelze otevírat hlasování, měnit účast, účastníky ani plné moci.
