# Prontto — Marketplace de Serviços Domésticos

Plataforma que conecta **contratantes** a **prestadores** de serviços domésticos na Grande São Paulo.
Monorepo com **backend .NET 9** (Clean Architecture + EF Core/MySQL) e **frontend Angular 21**.

- Produção: https://prontto.org

## Estrutura

```
backend/     API .NET 9 (Domain / Application / Infrastructure / Api) + testes
frontend/    App Angular 21 (standalone components, signals)
deploy/       Scripts de deploy do backend na VPS + checklist de QA
database/     Seeders / scripts de banco
```

## Pré-requisitos
- **.NET SDK 9**
- **Node.js 20+** e npm
- **MySQL/MariaDB 8+** rodando localmente

## Backend — rodar local
```bash
cd backend/src/Prontto.Api
# 1. crie seu appsettings de dev a partir do exemplo e preencha a senha do seu MySQL local:
cp appsettings.Development.json.example appsettings.Development.json
# 2. rode (as migrations aplicam automaticamente em dev):
dotnet run
```
API sobe em `http://localhost:5xxx` (veja o console). Testes: `dotnet test` na pasta `backend/`.

## Frontend — rodar local
```bash
cd frontend
npm install
npm start           # http://localhost:4200
```
O `environment.ts` (dev) aponta para a API local; `environment.production.ts` aponta para `https://api.prontto.org`.

## Build de produção
```bash
# frontend
cd frontend && npm run build          # saída em frontend/dist/prontto/browser
# backend (self-contained linux-x64)
dotnet publish backend/src/Prontto.Api/Prontto.Api.csproj -c Release -r linux-x64 --self-contained true -o backend/publish
```

## Deploy
Scripts em [`deploy/`](deploy/):
- `atualizar-backend.sh` — atualiza a API na VPS **preservando os uploads** (symlink persistente).
- `publish-backend.sh` — publica + envia + atualiza via SSH.
- `vps-deploy.sh` — provisionamento inicial (MySQL, nginx, systemd, TLS).
O frontend é publicado no File Manager (Hostinger) em `public_html`.

## Configuração / Segredos (NÃO commitar)
Estes arquivos são ignorados pelo git — cada dev/ambiente mantém o seu:
- `backend/src/Prontto.Api/appsettings.Development.json` → veja `.example`
- `.mcp.json` (token da Hostinger) → veja `.mcp.json.example`
- Segredos de **produção** ficam no servidor em `/etc/prontto/api.env` (não no repo).

## Fluxo de trabalho (branches, CI e deploy)
1. Crie um **branch** a partir da `main` e abra um **Pull Request**.
2. O **CI** (GitHub Actions — `.github/workflows/ci.yml`) roda automaticamente no PR: **build + testes do backend** (.NET 9) e **build do frontend** (Angular). O merge só deve ocorrer com o CI **verde**.
3. Após o merge na `main`, o CI roda de novo como sanidade.
4. **Deploy é manual**, feito pela equipe que guarda as chaves (SSH da VPS / FTP Hostinger) — o CI **não** faz deploy. Para publicar:
   - Backend: `bash deploy/publish-backend.sh` (ou envie o pacote e rode `deploy/atualizar-backend.sh` na VPS — preserva uploads).
   - Frontend: `npm run build` e suba `frontend/dist/prontto/browser/` para o `public_html` no File Manager da Hostinger.

> Recomendado: em **Settings → Branches** do GitHub, proteger a `main` exigindo o CI verde e ao menos 1 review antes do merge.

## Convenções
- Código e identificadores em **português** (padrão do projeto).
- Backend: Clean Architecture; status de serviço serializado em `snake_case`.
- Frontend: componentes standalone + signals; novo control flow (`@if/@for`).
