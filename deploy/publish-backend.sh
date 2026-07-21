#!/usr/bin/env bash
# ============================================================================
# Publica o backend self-contained (linux-x64) e envia para a VPS via SSH.
# Rode na SUA máquina (tem o .NET SDK). Requer chave SSH já configurada na VPS.
#
# Uso:
#   VPS_HOST=root@187.127.6.43 bash deploy/publish-backend.sh
# ============================================================================
set -euo pipefail

VPS_HOST="${VPS_HOST:?defina VPS_HOST=root@IP_DA_VPS}"
APP_DIR="${APP_DIR:-/opt/prontto/api}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${ROOT}/backend/publish"

echo "==> Publicando (Release, linux-x64, self-contained)"
rm -rf "${OUT}"
dotnet publish "${ROOT}/backend/src/Prontto.Api/Prontto.Api.csproj" \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=false -o "${OUT}"

echo "==> Empacotando build (tar.gz)"
PACOTE_LOCAL="${ROOT}/backend/prontto-api.tar.gz"
tar -czf "${PACOTE_LOCAL}" -C "${OUT}" .

echo "==> Enviando pacote + script de atualização para a VPS"
ssh "${VPS_HOST}" "mkdir -p ${APP_DIR}"
scp "${PACOTE_LOCAL}" "${VPS_HOST}:/tmp/prontto-api.tar.gz"
scp "${ROOT}/deploy/atualizar-backend.sh" "${VPS_HOST}:/tmp/atualizar-backend.sh"

echo "==> Atualizando na VPS (preserva uploads via symlink persistente)"
ssh "${VPS_HOST}" "APP_DIR=${APP_DIR} bash /tmp/atualizar-backend.sh"
echo "==> OK. Backend atualizado em ${APP_DIR} (uploads preservados)."
