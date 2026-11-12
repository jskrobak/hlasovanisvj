#!/usr/bin/env bash
# Nasadí zadaný commit (výchozí HEAD) na server: nahraje zdrojáky a přebuildí stack.
# Použití: deploy/deploy.sh [git-ref]      – nasadí commit (výchozí HEAD)
#          deploy/deploy.sh --worktree      – nasadí aktuální pracovní strom (i necommitnuté změny)
set -euo pipefail

HOST="${DEPLOY_HOST:-root@skrobak.net}"
DIR=/opt/hlasovanisvj
REF="${1:-HEAD}"

cd "$(git rev-parse --show-toplevel)"

if [ "$REF" = "--worktree" ]; then
  echo "Nasazuji pracovní strom ($(git rev-parse --short HEAD) + lokální změny) na $HOST"
  archive() { git ls-files -z --cached --others --exclude-standard | COPYFILE_DISABLE=1 tar --no-mac-metadata --no-xattrs --null -cf - -T -; }
else
  echo "Nasazuji $(git rev-parse --short "$REF") na $HOST"
  archive() { git archive --format=tar "$REF"; }
fi

archive | ssh "$HOST" "set -e
  rm -rf $DIR/src.new && mkdir -p $DIR/src.new
  tar -x -C $DIR/src.new
  rm -rf $DIR/src && mv $DIR/src.new $DIR/src"

ssh "$HOST" "set -e
  cd $DIR
  docker compose --env-file .env -f src/deploy/docker-compose.yml up -d --build --remove-orphans
  docker image prune -f >/dev/null
  docker compose --env-file .env -f src/deploy/docker-compose.yml ps"
