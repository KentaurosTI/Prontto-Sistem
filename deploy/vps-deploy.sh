#!/usr/bin/env bash
# ============================================================================
# Prontto — deploy do backend na VPS (Ubuntu). App .NET publicado self-contained
# (linux-x64), então NÃO é preciso instalar o runtime .NET.
#
# Uso (como root na VPS), depois de copiar o publish para /opt/prontto/api:
#   DOMAIN=api.prontto.org EMAIL=voce@dominio.com bash vps-deploy.sh
#
# Variáveis:
#   DOMAIN  (obrigatória) domínio/subdomínio público da API (A record -> IP da VPS)
#   EMAIL   (obrigatória p/ TLS) e-mail para o Let's Encrypt
#   APP_DIR (opcional) padrão /opt/prontto/api
# ============================================================================
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/prontto/api}"
DOMAIN="${DOMAIN:?defina DOMAIN=api.seudominio.com}"
EMAIL="${EMAIL:?defina EMAIL=voce@dominio.com}"
DB_NAME="prontto_prod"
DB_USER="prontto"
ENV_FILE="/etc/prontto/api.env"

echo "==> [1/7] Pacotes base (MySQL, nginx, certbot)"
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get install -y mysql-server nginx certbot python3-certbot-nginx openssl

echo "==> [2/7] Banco de dados MySQL"
systemctl enable --now mysql
# Gera senha do banco só na primeira vez (guarda em /etc/prontto)
mkdir -p /etc/prontto
if [ ! -f /etc/prontto/db_password ]; then
  openssl rand -base64 24 | tr -d '/+=' | head -c 32 > /etc/prontto/db_password
  chmod 600 /etc/prontto/db_password
fi
DB_PASS="$(cat /etc/prontto/db_password)"
mysql <<SQL
CREATE DATABASE IF NOT EXISTS ${DB_NAME} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
ALTER USER '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
GRANT ALL PRIVILEGES ON ${DB_NAME}.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL

echo "==> [3/7] Variáveis de ambiente da API (${ENV_FILE})"
# Gera SESSION_SECRET forte só na primeira vez
if [ ! -f /etc/prontto/session_secret ]; then
  openssl rand -hex 32 > /etc/prontto/session_secret
  chmod 600 /etc/prontto/session_secret
fi
SESSION_SECRET="$(cat /etc/prontto/session_secret)"
cat > "${ENV_FILE}" <<ENV
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:8080
AUTO_MIGRATE=true
ConnectionStrings__Default=Server=127.0.0.1;Port=3306;Database=${DB_NAME};User=${DB_USER};Password=${DB_PASS};
SESSION_SECRET=${SESSION_SECRET}
ENV
chmod 600 "${ENV_FILE}"

echo "==> [4/7] Executável self-contained"
if [ ! -f "${APP_DIR}/Prontto.Api" ]; then
  echo "ERRO: ${APP_DIR}/Prontto.Api não encontrado. Copie o publish antes de rodar." >&2
  exit 1
fi
chmod +x "${APP_DIR}/Prontto.Api"

echo "==> [5/7] Serviço systemd"
cat > /etc/systemd/system/prontto-api.service <<UNIT
[Unit]
Description=Prontto API (.NET)
After=network.target mysql.service

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=${APP_DIR}/Prontto.Api
EnvironmentFile=${ENV_FILE}
Restart=always
RestartSec=5
User=www-data
KillSignal=SIGINT
SyslogIdentifier=prontto-api

[Install]
WantedBy=multi-user.target
UNIT
chown -R www-data:www-data "${APP_DIR}"
systemctl daemon-reload
systemctl enable prontto-api
systemctl restart prontto-api

echo "==> [6/7] nginx (reverse proxy para ${DOMAIN})"
cat > /etc/nginx/sites-available/prontto-api <<NGINX
server {
    listen 80;
    server_name ${DOMAIN};
    client_max_body_size 6M;
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
NGINX
ln -sf /etc/nginx/sites-available/prontto-api /etc/nginx/sites-enabled/prontto-api
nginx -t && systemctl reload nginx

echo "==> [7/7] TLS (Let's Encrypt) — requer DNS de ${DOMAIN} apontando para esta VPS"
certbot --nginx -d "${DOMAIN}" --non-interactive --agree-tos -m "${EMAIL}" --redirect || \
  echo "AVISO: certbot falhou (provável DNS ainda não propagado). Rode depois: certbot --nginx -d ${DOMAIN}"

echo
echo "============================================================"
echo " Deploy concluído."
echo "  Serviço:  systemctl status prontto-api"
echo "  Logs:     journalctl -u prontto-api -f"
echo "  Banco:    ${DB_NAME} / usuário ${DB_USER} (senha em /etc/prontto/db_password)"
echo "  Teste:    curl -k https://${DOMAIN}/healthz"
echo "============================================================"
