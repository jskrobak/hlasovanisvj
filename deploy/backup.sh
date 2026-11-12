#!/usr/bin/env bash
# Denní záloha databáze (spouští cron na serveru), drží 14 dní.
set -euo pipefail
DIR=/opt/hlasovanisvj
mkdir -p "$DIR/backups"
cd "$DIR"
docker compose --env-file .env -f src/deploy/docker-compose.yml exec -T db \
  pg_dump -U hlasovanisvj -d hlasovanisvj --no-owner \
  | gzip > "$DIR/backups/hlasovanisvj-$(date +%F).sql.gz"
find "$DIR/backups" -name 'hlasovanisvj-*.sql.gz' -mtime +14 -delete
