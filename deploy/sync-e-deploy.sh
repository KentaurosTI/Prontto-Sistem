#!/usr/bin/env bash
# ============================================================================
# Prontto — Sincroniza com o GitHub, VALIDA (build + testes) e faz o deploy.
# Rode na máquina do time (que tem a chave SSH da VPS). Aborta a qualquer falha.
#
#   VPS_HOST=root@187.127.6.43 SSH_KEY=~/.ssh/prontto_vps bash deploy/sync-e-deploy.sh
#
# O que faz, em ordem (para se qualquer passo falhar):
#   1. git fetch + pull --ff-only da main (traz o que os outros devs mergearam)
#   2. Backend: build + testes unitários  (não deploya se um teste falhar)
#   3. Frontend: build de produção
#   4. Deploy do backend na VPS (preserva uploads, aplica migrations no startup)
#   5. Gera deploy/prontto-frontend.zip para subir no File Manager da Hostinger
# ============================================================================
set -euo pipefail

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
VPS_HOST="${VPS_HOST:-root@187.127.6.43}"
SSH_KEY="${SSH_KEY:-$HOME/.ssh/prontto_vps}"
APP_DIR="${APP_DIR:-/opt/prontto/api}"
cd "$RAIZ"

echo "==> [1/5] Sincronizando com o GitHub (git pull --ff-only origin main)"
git fetch origin
if [ -n "$(git status --porcelain)" ]; then
  echo "ERRO: há mudanças locais não commitadas. Commit/stash antes de sincronizar." >&2
  git status --short; exit 1
fi
git checkout main
git pull --ff-only origin main
echo "    main agora em: $(git rev-parse --short HEAD)"

echo "==> [2/5] Backend: build + testes (aborta se falhar)"
dotnet test "$RAIZ/backend/Prontto.sln" -c Release --nologo

echo "==> [3/5] Frontend: build de produção"
( cd "$RAIZ/frontend" && npm ci && npm run build )

echo "==> [4/5] Deploy do backend na VPS ($VPS_HOST)"
rm -rf "$RAIZ/backend/publish"
dotnet publish "$RAIZ/backend/src/Prontto.Api/Prontto.Api.csproj" \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false \
  -o "$RAIZ/backend/publish"
( cd "$RAIZ/backend/publish" && tar -czf ../prontto-api.tar.gz . )
scp -i "$SSH_KEY" -o IdentitiesOnly=yes "$RAIZ/backend/prontto-api.tar.gz" "$VPS_HOST:/tmp/prontto-api.tar.gz"
scp -i "$SSH_KEY" -o IdentitiesOnly=yes "$RAIZ/deploy/atualizar-backend.sh" "$VPS_HOST:/tmp/atualizar-backend.sh"
ssh -i "$SSH_KEY" -o IdentitiesOnly=yes "$VPS_HOST" "APP_DIR=$APP_DIR bash /tmp/atualizar-backend.sh"
echo "    healthz: $(curl -s -o /dev/null -w '%{http_code}' https://api.prontto.org/healthz)"

echo "==> [5/5] Gerando zip do frontend"
ZIP="$RAIZ/deploy/prontto-frontend.zip"
rm -f "$ZIP"
( cd "$RAIZ/frontend/dist/prontto/browser" && zip -qr "$ZIP" . )
echo "    Frontend pronto: $ZIP"

echo
echo "============================================================"
echo " Deploy concluído. Backend no ar (main $(git rev-parse --short HEAD))."
echo " Suba o CONTEÚDO de deploy/prontto-frontend.zip no public_html (File Manager)."
echo "============================================================"
