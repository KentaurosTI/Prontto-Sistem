#!/usr/bin/env bash
# ============================================================================
# Atualiza o backend na VPS PRESERVANDO os uploads (portfólio).
# Rode COMO ROOT na VPS, após enviar o pacote para /tmp/prontto-api.tar.gz.
#
#   bash atualizar-backend.sh
#
# Estratégia: as imagens ficam em /opt/prontto/uploads (persistente, FORA do
# diretório do app). O app acessa via symlink /opt/prontto/api/uploads.
# A cada deploy o diretório do app é limpo, mas o symlink é recriado — os
# arquivos persistentes nunca são tocados.
# ============================================================================
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/prontto/api}"
PERSIST_DIR="${PERSIST_DIR:-/opt/prontto/uploads}"
PACOTE="${PACOTE:-/tmp/prontto-api.tar.gz}"

[ -f "$PACOTE" ] || { echo "ERRO: $PACOTE não encontrado. Envie o pacote antes." >&2; exit 1; }

echo "==> [1/6] Garantindo diretório persistente de uploads"
mkdir -p "$PERSIST_DIR"
chown -R www-data:www-data "$PERSIST_DIR"

echo "==> [2/6] Parando serviço"
systemctl stop prontto-api

echo "==> [3/6] Limpando diretório do app (uploads persistentes NÃO são afetados)"
# Remove tudo do app, inclusive o symlink antigo de uploads (o alvo persiste).
rm -rf "${APP_DIR:?}"/*

echo "==> [4/6] Extraindo novo build"
tar -xzf "$PACOTE" -C "$APP_DIR"
chmod +x "$APP_DIR/Prontto.Api"

echo "==> [5/6] Recriando symlink de uploads -> persistente"
# Caso a extração tenha criado uma pasta 'uploads' real, mescla e remove.
if [ -d "$APP_DIR/uploads" ] && [ ! -L "$APP_DIR/uploads" ]; then
  cp -an "$APP_DIR/uploads/." "$PERSIST_DIR/" 2>/dev/null || true
  rm -rf "$APP_DIR/uploads"
fi
ln -sfn "$PERSIST_DIR" "$APP_DIR/uploads"
chown -R www-data:www-data "$APP_DIR"

echo "==> [6/6] Iniciando serviço"
systemctl start prontto-api
sleep 4
systemctl is-active prontto-api
echo "OK. uploads -> $(readlink -f "$APP_DIR/uploads")"
