# RF-10 — SEO, Hospedagem e Configurações Técnicas

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Definir os requisitos não-funcionais de publicação, segurança, SEO e configurações técnicas de infraestrutura da plataforma Prontto em ambiente de produção.

---

## Descrição

RF-10 cobre requisitos não-funcionais de infraestrutura e segurança: HTTPS obrigatório, CORS, rate limiting, validação de inputs, sanitização de outputs, configuração de variáveis de ambiente, SEO on-page e preparação para integrações futuras.

A infraestrutura de produção é baseada em **Docker Compose** com PostgreSQL 17, backend ASP.NET Core e frontend Angular. O deploy usa **NGINX como reverse proxy** com HTTPS. O gateway de pagamento é a **Pagar.me** — não AbacatePay nem Banda Pay. O ambiente de hospedagem específico (servidor/provedor) não está definido na arquitetura e é uma decisão de deployment operacional.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Equipe de Desenvolvimento | Configura infraestrutura, variáveis de ambiente e deploy |
| Sistema (NGINX) | Reverse proxy, HTTPS, roteamento |
| Sistema (ASP.NET Core) | Rate limiting, validação, sanitização, CORS |
| Sistema (Angular) | SEO on-page (meta tags, títulos, URLs amigáveis), sanitização de outputs |

---

## Pré-condições

- Domínio `prontto.com.br` configurado com DNS apontando para o servidor de produção.
- Certificado SSL/TLS configurado (Let's Encrypt ou equivalente).
- Variáveis de ambiente de produção definidas e seguras.
- Conta Pagar.me com credenciais de produção.
- Conta Cloudinary configurada com moderação habilitada.

---

## Fluxos Principais

### FP-01 — Deploy em Produção

1. Repositório com código-fonte na branch principal.
2. `docker-compose.yml` define: serviço `backend` (ASP.NET Core), `frontend` (Angular), `db` (PostgreSQL 17).
3. NGINX configurado como reverse proxy na frente do backend e frontend.
4. HTTPS configurado via NGINX com certificado SSL válido.
5. Variáveis de ambiente injetadas via arquivo `.env` ou secrets manager (nunca hardcoded).
6. Migrations EF Core executadas na inicialização (ou via pipeline separado).
7. Sistema acessível via `https://prontto.com.br`.

### FP-02 — Segurança em Produção

1. HTTPS obrigatório — todas as requisições HTTP redirecionadas para HTTPS via NGINX.
2. CORS configurado: origem permitida `https://prontto.com.br` (e `http://localhost:4200` em desenvolvimento).
3. Rate limiting ativo em todos os endpoints críticos (§10.5 da arquitetura).
4. Todos os inputs validados via `FluentValidation` antes de atingir a camada de domínio.
5. Queries ao banco via EF Core (parameterized queries) — sem risco de SQL injection.
6. Conteúdo de chat sanitizado no Angular com `DomSanitizer` antes da renderização.
7. CPF armazenado criptografado (AES-256) — nunca retornado pela API pública.
8. Secrets em variáveis de ambiente: `SESSION_SECRET`, `PAGARME_API_KEY`, `PAGARME_WEBHOOK_SECRET`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`.

### FP-03 — SEO On-Page

1. Frontend Angular implementa meta tags por rota:
   - `<title>` dinâmico por página
   - `<meta name="description">` com descrição relevante
   - Open Graph tags (`og:title`, `og:description`, `og:image`)
2. URLs amigáveis para todas as rotas públicas:
   - Perfis: `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`
   - Categorias: `/servicos/{categoriaSlug}`
   - Páginas públicas: `/como-funciona`, `/para-prestadores`
3. Rotas protegidas (autenticadas) e painel admin excluídos de indexação via `robots.txt` ou `noindex`.
4. Sitemap gerado para perfis públicos de Prestadores.

### FP-04 — Configuração de Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `SESSION_SECRET` | Segredo JWT HS256 (mínimo 32 chars em produção) |
| `DATABASE_URL` | Connection string PostgreSQL |
| `PAGARME_API_KEY` | Chave de API da Pagar.me |
| `PAGARME_WEBHOOK_SECRET` | Segredo para validação HMAC-SHA256 dos webhooks |
| `CLOUDINARY_CLOUD_NAME` | Nome da conta Cloudinary |
| `CLOUDINARY_API_KEY` | Chave de API Cloudinary |
| `CLOUDINARY_API_SECRET` | Segredo Cloudinary |
| `CORS_ORIGINS` | Origens permitidas (`https://prontto.com.br`) |

---

## Fluxos Alternativos

### FA-01 — Requisição HTTP em produção

- **Ação**: NGINX redireciona automaticamente para HTTPS (301 Redirect).

### FA-02 — Requisição de origem não permitida (CORS)

- **Ação**: ASP.NET Core retorna `403 Forbidden` para requisições de origens fora da lista de CORS.

---

## Fluxos de Exceção

### FE-01 — Variável de ambiente não configurada

- **Ponto de desvio**: Aplicação inicia sem variável crítica (ex: `SESSION_SECRET`).
- **Ação**: Aplicação deve falhar na inicialização com erro descritivo (`throw InvalidOperationException`). **Nunca usar valor default hardcoded em produção.**

---

## Pós-condições

- Sistema acessível via HTTPS no domínio configurado.
- Páginas públicas indexáveis pelo Google.
- Rate limiting e validações ativas.
- Todos os secrets em variáveis de ambiente.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | HTTPS obrigatório em produção via NGINX. Nenhum dado trafega em HTTP. |
| RN-02 | CORS permite apenas `https://prontto.com.br` em produção (e `http://localhost:4200` em desenvolvimento). |
| RN-03 | **Nenhum secret** (JWT, Pagar.me, Cloudinary) hardcoded no código. Todos via variáveis de ambiente. |
| RN-04 | Todos os inputs do usuário validados com `FluentValidation` antes de atingir a camada de domínio. |
| RN-05 | EF Core com parameterized queries — prevenção nativa de SQL injection. |
| RN-06 | Conteúdo de chat sanitizado no Angular com `DomSanitizer` — prevenção de XSS. |
| RN-07 | CPF armazenado criptografado (AES-256) no banco — nunca retornado pela API pública. |
| RN-08 | Rate limiting implementado com `Microsoft.AspNetCore.RateLimiting` (.NET 7+). |
| RN-09 | Contrato de API definido em `api-spec/openapi.yaml` (OpenAPI 3.1) como fonte da verdade (ADR-06). Qualquer mudança de endpoint começa no YAML. |
| RN-10 | Arquitetura modular (Clean Architecture + bounded contexts) permite adição de features sem refatoração estrutural. |
| RN-11 | Pool de conexões PostgreSQL configurado: `Max Pool Size=100` na connection string do EF Core. |

---

## Configuração de Rate Limiting

| Endpoint | Limite |
|---------|--------|
| `POST /api/auth/login` | 10 req / minuto por IP |
| `POST /api/auth/register` | 5 req / minuto por IP |
| `POST /api/chat/{id}/mensagens` | 30 req / minuto por usuário |
| Geral | 200 req / minuto por IP |

---

## Eventos de Domínio

Nenhum. RF-10 descreve configurações de infraestrutura e não gera eventos de domínio.

---

## Entidades Envolvidas

Nenhuma entidade de negócio específica. RF-10 é transversal à aplicação.

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/healthz` | Público | Health check (monitoramento) |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Sistema acessível via HTTPS no domínio configurado após deploy. |
| CA-02 | Requisição HTTP redirecionada para HTTPS automaticamente. |
| CA-03 | Páginas públicas com meta tags corretas e indexáveis pelo Google. |
| CA-04 | Rotas autenticadas não indexadas (`noindex` ou exclusão via `robots.txt`). |
| CA-05 | Rate limiting bloqueia com `429` nos limites definidos. |
| CA-06 | Nenhum secret hardcoded — todos em variáveis de ambiente. |
| CA-07 | Aplicação não inicia sem variáveis de ambiente críticas. |
| CA-08 | `/healthz` retorna `200 OK` quando o serviço está saudável. |
| CA-09 | CORS bloqueia requisições de origens não permitidas. |
| CA-10 | `openapi.yaml` atualizado em conjunto com qualquer mudança de API. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Acesso via HTTP em produção | Redirecionamento 301 para HTTPS |
| CT-02 | Requisição com `Origin: https://outro-dominio.com` | `403 Forbidden` (CORS bloqueado) |
| CT-03 | Página de perfil público com meta tags | `<title>` e `<meta description>` presentes no HTML |
| CT-04 | `/healthz` com serviço ativo | `200 OK` |
| CT-05 | Inicialização sem `SESSION_SECRET` | Falha na inicialização com erro descritivo |
| CT-06 | 11ª requisição de login no mesmo IP em 1 min | `429 Too Many Requests` |
| CT-07 | CPF retornado pela API pública | Ausente na resposta (nunca retornado) |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Deploy no Hostinger especificamente | RF-10 PDF | Arquitetura define Docker Compose + NGINX. O provedor de hospedagem é decisão operacional não definida na arquitetura. |
| Gateway AbacatePay ou Banda Pay | RF-10 PDF | Arquitetura define Pagar.me como gateway (ADR-01). |
| E-mail corporativo configurado via provedor | RF-10 PDF | Configuração de e-mail não é documentada na arquitetura técnica. |
| Integração com CRM | RF-10 PDF | Mencionado como "futuramente". Não documentado na arquitetura V1. |
| Integração com WhatsApp para notificações | RF-10 PDF | Mencionado como "futuramente". Arquitetura V1 documenta notificações in-app. |
| CI/CD pipeline específico | RF-10 PDF | Sprint 8 da arquitetura menciona CI/CD (GitHub Actions ou Azure DevOps) mas sem definição detalhada. |
