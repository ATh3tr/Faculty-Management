#!/usr/bin/env bash
set -euo pipefail

compose=(docker compose -f docker-compose.yml -f docker-compose.demo.yml)

if [[ ! -f .env ]]; then
  bash .devcontainer/setup-demo.sh
fi

"${compose[@]}" up --build --detach

printf '\nWaiting for the web application to become healthy'
for _ in $(seq 1 90); do
  if curl --fail --silent http://localhost:3000/health >/dev/null 2>&1; then
    printf '\n'
    break
  fi
  printf '.'
  sleep 2
done

if ! curl --fail --silent http://localhost:3000/health >/dev/null 2>&1; then
  printf '\nThe application did not become healthy. Recent logs:\n'
  "${compose[@]}" logs --tail 120
  exit 1
fi

if [[ -n "${CODESPACE_NAME:-}" ]] && command -v gh >/dev/null 2>&1; then
  gh codespace ports visibility 3000:public --codespace "${CODESPACE_NAME}" >/dev/null
  public_url="https://${CODESPACE_NAME}-3000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
else
  public_url="http://localhost:3000"
fi

printf '\nDemo is ready: %s\n\n' "${public_url}"
printf 'Accounts (all non-admin accounts use Demo123!):\n'
printf '  Admin:         admin@faculty.demo / AdminDemo123!\n'
printf '  Professor:     professor@faculty.demo / Demo123!\n'
printf '  Teacher:       teacher@faculty.demo / Demo123!\n'
printf '  Exams officer: exams@faculty.demo / Demo123!\n'
printf '  Student:       student1@faculty.demo / Demo123!\n\n'

