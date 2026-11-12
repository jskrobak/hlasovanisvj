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

Tajné hodnoty patří do user-secrets (lokálně) nebo proměnných prostředí (produkce, `:` → `__`).

- `BaseUrl` – veřejná adresa aplikace; použije se v QR kódech na lístcích a v odkazech pro obnovení hesla.
- `Smtp:Host`, `Smtp:Port` (výchozí 587), `Smtp:UserName`, `Smtp:Password`, `Smtp:From`, `Smtp:FromName`
  – odesílání e-mailů (obnovení hesla). Bez `Smtp:Host` se e-maily jen zapisují do logu (vývoj).
- `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` – přihlášení a registrace přes Google.
  Bez nich se tlačítka Google nezobrazují. V Google Cloud Console (OAuth client typu *Web application*)
  nastavte *Authorized redirect URI* na `https://<doména>/signin-google`.

Za reverse proxy aplikace čte `X-Forwarded-Proto/Host/For` – proxy je musí posílat a aplikace
nemá být dostupná přímo, jen přes proxy.

## Nasazení (produkce)

Produkce běží na `skrobak.net` (ARM64) v Docker Compose v `/opt/hlasovanisvj`:
PostgreSQL, aplikace (image se builduje na serveru) a Caddy s automatickým HTTPS.
Hlavní doména je `https://schuzesvj.cz`; `schuzesvj.eu` a `www.schuzesvj.eu` na ni přesměrují.

- Jednorázová příprava serveru: `ssh root@skrobak.net 'bash -s' < deploy/server-setup.sh`
- Nasazení commitu (výchozí `HEAD`): `deploy/deploy.sh [git-ref]`; necommitnutý stav: `deploy/deploy.sh --worktree`
- Tajné hodnoty (DB heslo, SMTP, Google) jsou jen na serveru v `/opt/hlasovanisvj/.env`; po úpravě znovu spustit `deploy/deploy.sh`.
- Záloha DB: denně ve 2:30 do `/opt/hlasovanisvj/backups` (14 dní), viz `deploy/backup.sh`.
- Logy: `ssh root@skrobak.net 'cd /opt/hlasovanisvj && docker compose --env-file .env -f src/deploy/docker-compose.yml logs -f app'`

## Schůze – detail a report

Na stránce **Schůze → Detail** je souhrn, usnesení s výsledky a prezenční listina.
Tlačítko **Stáhnout PDF** vytvoří report pro archivaci (souhrn, usnesení, prezenční listina
s místem pro podpis a jmenovité hlasování ke každému usnesení).

## Schůze

- Účastníci, usnesení i hlasy patří vždy ke konkrétní schůzi; import nové schůze starší schůze neovlivní.
- Uzavřená schůze je jen pro čtení: výsledky usnesení (zafixované při uzavření hlasování) lze prohlížet,
  ale nelze otevírat hlasování, měnit účast, účastníky ani plné moci.
