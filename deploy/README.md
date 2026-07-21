# Deploy do Prontto — Produção

Arquitetura escolhida:
- **Backend (.NET 9)** → VPS Hostinger (Ubuntu), IP `187.127.6.43`, publicado **self-contained** (não precisa instalar .NET na VPS). Exposto em **`https://api.prontto.org`** via nginx + TLS.
- **Frontend (Angular)** → **hospedagem** do domínio, servido em **`https://prontto.org`** (build estático enviado pelo File Manager).
- CORS do backend já libera `https://prontto.org` e `https://www.prontto.org`.

---

## Pré-requisitos (uma vez)

1. **DNS**: criar um registro **A** `api.prontto.org` → `187.127.6.43` (na zona DNS do domínio na Hostinger). Aguarde propagar.
2. **Acesso SSH por chave** à VPS (não use senha):
   - Gere um par: `ssh-keygen -t ed25519 -f ~/.ssh/prontto_vps -N ""`
   - Adicione a **chave pública** (`~/.ssh/prontto_vps.pub`) na VPS: hPanel → VPS → **Chave SSH → Gerenciar**, ou via MCP (`VPS_createPublicKeyV1` + attach).
   - Teste: `ssh -i ~/.ssh/prontto_vps root@187.127.6.43`

---

## Backend (VPS)

Na **sua máquina** (tem o .NET SDK 9):

```bash
# 1) Publica self-contained e envia para a VPS
VPS_HOST=root@187.127.6.43 bash deploy/publish-backend.sh
```

Na **VPS** (via SSH, como root):

```bash
# 2) Instala MySQL + nginx + TLS, cria o serviço systemd e sobe a API
#    (gera SESSION_SECRET e senha do banco automaticamente)
DOMAIN=api.prontto.org EMAIL=seu-email@dominio.com bash /opt/prontto/api/../vps-deploy.sh
# (copie deploy/vps-deploy.sh para a VPS antes, ex.: scp deploy/vps-deploy.sh root@187.127.6.43:/root/)
```

Verificação:
```bash
curl https://api.prontto.org/healthz            # 200
curl https://api.prontto.org/api/categorias     # lista as 11 categorias (seed)
journalctl -u prontto-api -f                     # logs
```

> As migrations rodam sozinhas no startup (`AUTO_MIGRATE=true` no `api.env`), incluindo o seed de categorias/cidades.

---

## Frontend (hospedagem)

Na **sua máquina**:

```bash
# 1) Build de produção (usa environment.production.ts -> apiUrl https://api.prontto.org)
cd frontend
npm ci
npx ng build --configuration production
```

2. O build fica em **`frontend/dist/prontto/browser/`**.
3. No **File Manager** da hospedagem, envie **todo o conteúdo de `browser/`** (incluindo o `.htaccess`) para a pasta pública do domínio (`public_html`).
4. Verifique `https://prontto.org` — a Home deve carregar e as chamadas devem ir para `https://api.prontto.org/api/...`.

---

## Pós-deploy / pendências

- **Stripe**: ainda não integrado (backend usa Pagar.me/stub). Integrar depois com `PAYMENT_PROVIDER`.
- **Segurança**: regenerar o token da API Hostinger e trocar a senha root da VPS (foram expostos no chat).
- **Firewall** da VPS: liberar 80/443, restringir o resto.
- **Backups**: já há snapshots semanais; avaliar backup do MySQL (`mysqldump` em cron).
