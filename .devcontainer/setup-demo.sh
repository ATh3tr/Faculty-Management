#!/usr/bin/env bash
set -euo pipefail

if [[ ! -f .env ]]; then
  codespace_domain="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
  if [[ -n "${CODESPACE_NAME:-}" ]]; then
    public_origin="https://${CODESPACE_NAME}-3000.${codespace_domain}"
  else
    public_origin="http://localhost:3000"
  fi

  sql_password="FacultySql_$(openssl rand -hex 12)!"
  jwt_key="$(openssl rand -hex 32)"

  printf '%s\n' \
    "FACULTY_DB_PASSWORD=${sql_password}" \
    "FACULTY_JWT_SIGNING_KEY=${jwt_key}" \
    "FACULTY_REACT_ORIGIN=${public_origin}" \
    "FACULTY_ADMIN_EMAIL=admin@faculty.demo" \
    "FACULTY_ADMIN_PASSWORD=AdminDemo123!" \
    "FACULTY_DEMO_PASSWORD=Demo123!" > .env
fi

docker compose -f docker-compose.yml -f docker-compose.demo.yml config --quiet

printf '\nCodespace setup is ready. Start the populated demo with:\n\n'
printf '  bash .devcontainer/start-demo.sh\n\n'

