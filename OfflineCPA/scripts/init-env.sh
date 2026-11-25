#!/bin/sh
# Generates OfflineCPA/.env with a random database password on first run.
set -eu

cd "$(dirname "$0")/.."

if [ -f .env ]; then
    echo ".env already exists, not overwriting."
    exit 0
fi

if command -v openssl >/dev/null 2>&1; then
    PASSWORD=$(openssl rand -hex 16)
else
    PASSWORD=$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')
fi

cat > .env <<EOF
POSTGRES_PASSWORD=$PASSWORD
CONNECTION_STRING=Host=localhost;Port=5432;Database=OfflineCPADb;Username=bookkeeping;Password=$PASSWORD
EOF
chmod 600 .env

echo "Wrote .env with a generated password (mode 600)."
