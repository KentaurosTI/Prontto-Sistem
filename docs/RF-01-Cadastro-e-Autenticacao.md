# RF-01 — Cadastro e Autenticação

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Permitir que Clientes e Prestadores criem contas na plataforma Prontto, realizem login seguro e mantenham sessões autenticadas com renovação de token.

---

## Descrição

O sistema oferece dois fluxos de cadastro distintos: **Cliente** (quem contrata) e **Prestador** (quem executa). O usuário escolhe seu perfil ao se registrar. O acesso é segmentado por papel (`Papel`), garantindo que cada tipo de usuário acesse apenas as funcionalidades pertinentes ao seu papel.

A autenticação é baseada em **JWT HS256** com par de tokens: Access Token de curta duração (15 minutos) e Refresh Token de longa duração (30 dias) com rotação obrigatória. O Refresh Token é armazenado em cookie `HttpOnly; Secure; SameSite=Strict`.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente | Usuário com `TipoConta = Cliente` que se cadastra para contratar serviços |
| Prestador | Usuário com `TipoConta = Prestador` que se cadastra para executar serviços |
| Sistema | Geração de tokens, validação de unicidade de e-mail, emissão de eventos |

---

## Pré-condições

- O e-mail fornecido não deve estar previamente cadastrado na plataforma.
- Campos obrigatórios do cadastro devem estar presentes e válidos.

---

## Fluxos Principais

### FP-01 — Cadastro de Cliente

1. Usuário acessa `/cadastrar` e seleciona o perfil "Cliente".
2. Usuário preenche: nome, e-mail, senha e cidade (seleção por entidade `Cidade`, não texto livre).
3. Sistema valida os dados via `FluentValidation`:
   - E-mail único e formato válido.
   - Senha com mínimo de 8 caracteres.
   - `CidadeId` deve referenciar uma `Cidade` ativa existente no banco.
4. Sistema cria o registro `Usuario` com:
   - `TipoConta = Cliente`
   - `Papel = Usuario`
   - `HashSenha` gerado via BCrypt
5. Sistema publica evento `UsuarioCadastrado`.
6. Sistema emite Access Token (15 min) + Refresh Token (30 dias, armazenado como hash SHA-256 na tabela `tokens_renovacao`).
7. Refresh Token enviado via cookie `HttpOnly; Secure; SameSite=Strict`.
8. Access Token retornado no corpo da resposta.
9. Frontend redireciona para a área do cliente.

### FP-02 — Cadastro de Prestador

1. Usuário acessa `/cadastrar` e seleciona o perfil "Prestador".
2. Usuário preenche: nome, e-mail e senha.
3. Sistema valida os dados via `FluentValidation` (mesmas regras do Cliente).
4. Sistema cria o registro `Usuario` com:
   - `TipoConta = Prestador`
   - `Papel = Usuario`
   - `HashSenha` gerado via BCrypt
   - Campos de perfil público (`Slug`, categorias, cidades, dados bancários) **não são preenchidos no cadastro** — são completados posteriormente via PR-02 e PR-04.
5. Sistema publica evento `UsuarioCadastrado`.
6. Sistema emite Access Token + Refresh Token (mesmo fluxo do FP-01).
7. Frontend redireciona para a área do prestador, solicitando completar perfil.

### FP-03 — Login

1. Usuário acessa `/entrar` e preenche e-mail e senha.
2. Sistema valida credenciais: busca `Usuario` por e-mail (case-insensitive), verifica `HashSenha` via BCrypt.
3. Sistema verifica que o usuário não está com `DeletadoEm` preenchido (soft delete).
4. Sistema emite Access Token (15 min) + novo Refresh Token (30 dias).
5. Refresh Token anterior (se existir) é revogado (`RevogadoEm = now`).
6. Novo Refresh Token armazenado como hash SHA-256 na tabela `tokens_renovacao` com `Ip` e `UserAgent` registrados.
7. Sistema grava `AuditLog` com `Acao = "usuario.login"`.
8. Frontend recebe Access Token no corpo; Refresh Token via cookie `HttpOnly`.

### FP-04 — Renovação de Sessão (Refresh)

1. Frontend detecta resposta `401 Unauthorized` em qualquer requisição autenticada.
2. Frontend faz `POST /api/auth/refresh` enviando o cookie Refresh Token automaticamente.
3. Backend localiza o `RefreshToken` pelo hash SHA-256 do valor recebido.
4. Backend valida: `ExpiracaoEm > UtcNow` e `RevogadoEm IS NULL`.
5. Backend emite novo Access Token + novo Refresh Token.
6. Token anterior marcado como `RevogadoEm = now`, `SubstituidoPor = novoHash`.
7. Frontend retenta a requisição original com o novo Access Token.

### FP-05 — Logout

1. Usuário aciona "Sair" na interface.
2. Frontend faz `POST /api/auth/logout`.
3. Backend localiza o Refresh Token do cookie e define `RevogadoEm = now`.
4. Sistema grava `AuditLog` com `Acao = "usuario.logout"`.
5. Cookie Refresh Token é removido.
6. Frontend limpa o Access Token e redireciona para `/entrar`.

---

## Fluxos Alternativos

### FA-01 — E-mail já cadastrado

- **Ponto de desvio**: Passo 3 do FP-01 ou FP-02.
- **Ação**: Sistema retorna `409 Conflict` com mensagem "E-mail já cadastrado".
- **Continuação**: Usuário corrige o e-mail ou acessa `/entrar`.

### FA-02 — Refresh Token expirado

- **Ponto de desvio**: Passo 4 do FP-04 — `ExpiracaoEm <= UtcNow`.
- **Ação**: Backend retorna `401 Unauthorized`.
- **Continuação**: Frontend redireciona para `/entrar`. Usuário deve realizar novo login.

### FA-03 — Refresh Token revogado

- **Ponto de desvio**: Passo 4 do FP-04 — `RevogadoEm IS NOT NULL`.
- **Ação**: Backend retorna `401 Unauthorized`. Sistema grava `AuditLog` de alerta (possível comprometimento de sessão).
- **Continuação**: Frontend redireciona para `/entrar`.

---

## Fluxos de Exceção

### FE-01 — Dados inválidos no cadastro

- **Ponto de desvio**: Passo 3 do FP-01 ou FP-02.
- **Ação**: Sistema retorna `400 Bad Request` com detalhes dos campos inválidos (`FluentValidation`).
- **Continuação**: Usuário corrige os dados e resubmete.

### FE-02 — Credenciais inválidas no login

- **Ponto de desvio**: Passo 2 do FP-03 — e-mail não encontrado ou senha incorreta.
- **Ação**: Sistema retorna `401 Unauthorized` com mensagem genérica ("E-mail ou senha incorretos") — sem revelar qual campo está errado.
- **Continuação**: Usuário corrige as credenciais.

### FE-03 — Limite de rate limiting atingido

- **Ponto de desvio**: Tentativas excessivas em `POST /api/auth/login` ou `POST /api/auth/register`.
- **Limites**: Login — 10 req/min por IP; Registro — 5 req/min por IP.
- **Ação**: Sistema retorna `429 Too Many Requests`.
- **Continuação**: Usuário aguarda o intervalo de cooldown.

---

## Pós-condições

- Conta criada com sucesso: registro `Usuario` persistido com `TipoConta` correto e `Papel = Usuario`.
- Evento `UsuarioCadastrado` publicado (payload: `UsuarioId`, `Email`, `TipoConta`).
- Sessão ativa: Access Token válido por 15 minutos, Refresh Token válido por 30 dias no banco.
- `AuditLog` gravado para login e logout.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | O campo `Email` é único na tabela `usuarios` (constraint `UNIQUE`). Tentativa de cadastro com e-mail existente retorna `409 Conflict`. |
| RN-02 | A senha é armazenada exclusivamente como hash BCrypt. O valor bruto nunca é persistido ou logado. |
| RN-03 | O Refresh Token é armazenado no banco como **hash SHA-256** — nunca o valor bruto. |
| RN-04 | O Access Token expira em **15 minutos**. O Refresh Token expira em **30 dias**. |
| RN-05 | A cada renovação de sessão, o Refresh Token anterior é revogado (`RevogadoEm = now`) e um novo é emitido (**rotação obrigatória**). |
| RN-06 | O Refresh Token é transmitido exclusivamente via cookie `HttpOnly; Secure; SameSite=Strict`. Nunca exposto no corpo de resposta da API. |
| RN-07 | Usuário com `DeletadoEm` preenchido (soft delete) não pode realizar login — retorna `401`. |
| RN-08 | Categorias e cidades são referenciadas por FK (`CategoriaId`, `CidadeId`) — nunca por string livre. |
| RN-09 | **Dados bancários (PIX) NÃO são coletados no cadastro do Prestador.** São cadastrados posteriormente via caso de uso PR-04. |
| RN-10 | O `Papel` do usuário Admin é definido diretamente no banco por operador da plataforma. Não existe endpoint público para criar contas Admin. |
| RN-11 | O `Slug` do Prestador NÃO é gerado no cadastro. É gerado ao completar o perfil (PR-02) e é **imutável após publicação** (ADR-09). |
| RN-12 | O campo `Cpf`, quando preenchido, é armazenado criptografado (AES-256) e nunca retornado pela API pública. |
| RN-13 | O `Ip` e `UserAgent` são registrados no `RefreshToken` no momento do login para rastreabilidade. |
| RN-14 | O uso de um Refresh Token já revogado gera `AuditLog` de alerta — sinal de possível comprometimento de sessão. |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `UsuarioCadastrado` | Conta criada com sucesso | `{ UsuarioId, Email, TipoConta }` |
| `RefreshTokenRevogado` | Logout ou rotação de token | `{ UsuarioId, TokenHash }` |

---

## Entidades Envolvidas

### `Usuario`

**Tabela**: `usuarios`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK, UUID v4 |
| `Nome` | `string` | `name` | Obrigatório |
| `Email` | `string` | `email` | Único, lowercase |
| `Telefone` | `string?` | `phone` | Opcional |
| `HashSenha` | `string` | `password_hash` | BCrypt |
| `TipoConta` | `TipoConta` | `account_type` | `cliente` / `prestador` |
| `Papel` | `Papel` | `role` | `usuario` / `admin` |
| `CidadeId` | `Guid?` | `city_id` | FK → `cidades.id` |
| `Cpf` | `string?` | `cpf` | Criptografado AES-256 |
| `Slug` | `string?` | `slug` | Imutável após publicação (gerado em PR-02) |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `AtualizadoEm` | `DateTime` | `updated_at` | UTC |
| `DeletadoEm` | `DateTime?` | `deleted_at` | Soft delete |

### `RefreshToken`

**Tabela**: `tokens_renovacao`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `user_id` | FK → `usuarios.id` |
| `Token` | `string` | `token` | Hash SHA-256 do valor bruto |
| `ExpiracaoEm` | `DateTime` | `expires_at` | UTC; `now + 30 dias` |
| `RevogadoEm` | `DateTime?` | `revoked_at` | UTC; preenchido ao revogar |
| `SubstituidoPor` | `string?` | `replaced_by_token` | Hash do token substituto |
| `Ip` | `string?` | `ip_address` | IP do login |
| `UserAgent` | `string?` | `user_agent` | Device fingerprint |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

### `AuditLog`

**Tabela**: `logs_auditoria`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid?` | `user_id` | FK → `usuarios.id` |
| `Acao` | `string` | `action` | Ex: `usuario.login`, `usuario.cadastro` |
| `Entidade` | `string` | `entity` | Ex: `Usuario` |
| `EntidadeId` | `string?` | `entity_id` | ID do usuário afetado |
| `Ip` | `string?` | `ip_address` | IP da requisição |
| `UserAgent` | `string?` | `user_agent` | Header User-Agent |
| `Detalhes` | `string?` | `details` | JSON com contexto adicional |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

---

## API Endpoints

| Método | Rota | Auth | Caso de Uso |
|--------|------|------|-------------|
| `POST` | `/api/auth/register` | Público | FP-01, FP-02 |
| `POST` | `/api/auth/login` | Público | FP-03 |
| `POST` | `/api/auth/refresh` | Cookie Refresh Token | FP-04 |
| `POST` | `/api/auth/logout` | Bearer | FP-05 |
| `GET` | `/api/auth/me` | Bearer | Perfil do usuário logado |

**Rate Limiting**:
- `POST /api/auth/login`: 10 req/min por IP
- `POST /api/auth/register`: 5 req/min por IP

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Cadastro com e-mail já existente retorna `409 Conflict`. |
| CA-02 | Login com credenciais corretas emite Access Token (JWT, 15 min) e cookie com Refresh Token (30 dias). |
| CA-03 | Login com credenciais incorretas retorna `401` sem revelar qual campo está errado. |
| CA-04 | Refresh com token válido emite novo par de tokens e invalida o anterior. |
| CA-05 | Refresh com token expirado retorna `401`. |
| CA-06 | Refresh com token revogado retorna `401` e grava `AuditLog` de alerta. |
| CA-07 | Logout revoga o Refresh Token e grava `AuditLog`. |
| CA-08 | Cadastro de Prestador NÃO coleta dados bancários — apenas nome, e-mail e senha. |
| CA-09 | Acesso Admin não é acessível por clientes ou prestadores comuns (`Papel = Usuario`). |
| CA-10 | Rate limiting de login bloqueia com `429` após 10 tentativas/min por IP. |
| CA-11 | `AuditLog` é gravado para `usuario.login`, `usuario.logout` e `usuario.cadastro`. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Cadastro de Cliente com dados válidos | `201 Created`, Access Token no corpo, cookie Refresh Token, evento `UsuarioCadastrado` publicado |
| CT-02 | Cadastro de Prestador com dados válidos | `201 Created`, Access Token no corpo, sem dados bancários |
| CT-03 | Cadastro com e-mail duplicado | `409 Conflict` |
| CT-04 | Cadastro com senha de 7 caracteres | `400 Bad Request` (validação) |
| CT-05 | Login com credenciais corretas | `200 OK`, par de tokens emitido, `AuditLog` gravado |
| CT-06 | Login com senha errada | `401 Unauthorized` |
| CT-07 | Refresh com token válido | `200 OK`, novo par emitido, token anterior revogado |
| CT-08 | Refresh com token expirado | `401 Unauthorized` |
| CT-09 | Logout | `200 OK`, token revogado, `AuditLog` gravado |
| CT-10 | Acesso a `/api/admin/*` com `Papel = Usuario` | `403 Forbidden` |
| CT-11 | Login após soft delete do usuário | `401 Unauthorized` |
| CT-12 | 11ª tentativa de login no mesmo IP em 1 min | `429 Too Many Requests` |

---

## Escopo Negativo (o que NÃO está na arquitetura)

> Itens presentes no PDF original mas **não documentados** no `ARCHITECTURE.md`. NÃO devem ser implementados sem nova decisão arquitetural.

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Login social (Google, Facebook, Apple) | RF-01 PDF | Não documentado na arquitetura. Exige novo ADR e integração OAuth2. |
| Prestador informa dados bancários no cadastro | RF-01 PDF | Arquitetura define dados bancários como cadastro posterior (PR-04), não no registro inicial. |
| Aprovação manual de cadastros pelo Admin | RF-01 PDF | Não existe fluxo de aprovação de cadastro na arquitetura. Cadastro é automático. |
| Segmentação por "Contratante" | RF-01 PDF | O termo correto na arquitetura é "Cliente" (`TipoConta = Cliente`). |
