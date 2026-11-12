#!/usr/bin/env bash
# Jednorázová příprava serveru (Ubuntu). Spouští se jako root: ssh root@server 'bash -s' < deploy/server-setup.sh
set -euo pipefail
DIR=/opt/hlasovanisvj

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y docker.io docker-compose-v2
systemctl enable --now docker

mkdir -p "$DIR/backups"
if [ ! -f "$DIR/.env" ]; then
  umask 077
  cat > "$DIR/.env" <<ENV
BASE_URL=https://schuzesvj.cz
PG_PASSWORD=$(openssl rand -hex 24)

# Odesílání e-mailů (obnovení hesla) – bez SMTP_HOST se e-maily jen logují
SMTP_HOST=
SMTP_PORT=587
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_FROM=noreply@schuzesvj.cz

# Přihlášení přes Google – redirect URI: https://schuzesvj.cz/signin-google
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
ENV
  echo "Vytvořen $DIR/.env"
fi

cat > /etc/cron.d/hlasovanisvj-backup <<CRON
30 2 * * * root bash $DIR/src/deploy/backup.sh >> /var/log/hlasovanisvj-backup.log 2>&1
CRON
