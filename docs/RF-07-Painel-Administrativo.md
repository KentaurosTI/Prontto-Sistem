# RF-07 — Painel Administrativo

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Fornecer ao usuário Admin uma interface de controle completa para monitorar métricas, gerenciar usuários, serviços, cobranças, disputas, imagens pendentes de moderação e consultar trilha de auditoria.

---

## Descrição

O painel administrativo é exclusivo para usuários com `Papel = Admin`. O controle de acesso é baseado na claim JWT `role = admin` — não em e-mail específico. O Admin pode executar 12 casos de uso (AD-01 a AD-12) cobrindo as principais operações de governança da plataforma.

O painel inclui: estatísticas gerais, gestão de usuários e serviços, extrato financeiro, chat de auditoria, moderação de imagens, resolução de disputas e trilha de auditoria (`AuditLog`).

---

## Atores

| Ator | Descrição |
|------|-----------|
| Admin | Usuário com `Papel = Admin`; único ator com acesso ao painel |
| Sistema | Fornece dados agregados, processa transições acionadas pelo Admin |

---

## Pré-condições

- Usuário autenticado com `Papel = Admin` (claim `role = admin` no JWT).
- Token JWT válido (não expirado).

---

## Fluxos Principais

### FP-01 — Ver Estatísticas Gerais (AD-01)

1. Admin acessa o dashboard (`/admin`).
2. Sistema retorna `EstatisticasAdmin` (cacheado por 5 minutos):
   - Total de usuários cadastrados
   - Total de Clientes e Prestadores
   - Total de serviços por status
   - Receita total (soma de `Cobranca.ValorTotal` onde `Status IN (Retido, Liberado)`)
   - Taxa acumulada da plataforma (soma de `Cobranca.TaxaAdmin` onde `Status IN (Retido, Liberado)`)
   - Serviços concluídos no mês corrente

### FP-02 — Listar e Filtrar Usuários (AD-02)

1. Admin acessa a seção de usuários.
2. Admin aplica filtros: `TipoConta` (Cliente/Prestador), especialidade (texto), `CidadeId`.
3. Sistema retorna lista paginada de usuários com filtros aplicados.
4. Admin pode visualizar detalhes de um usuário específico.

### FP-03 — Bloquear / Desbloquear Usuário (AD-09)

1. Admin seleciona um usuário.
2. Admin aciona "Bloquear" ou "Desbloquear".
3. Sistema executa soft delete (`DeletadoEm = now`) para bloquear, ou limpa `DeletadoEm` para desbloquear.
4. Ao bloquear: todos os Refresh Tokens do usuário são revogados.
5. `AuditLog` gravado: `Acao = "admin.usuario.bloqueado"` ou `"admin.usuario.desbloqueado"`.

### FP-04 — Listar e Gerenciar Serviços (AD-03 / AD-04)

1. Admin acessa a seção de serviços.
2. Admin filtra por status, data, usuário ou categoria.
3. Sistema retorna lista paginada de serviços.
4. Admin pode alterar manualmente o status de um serviço (transições permitidas pelo Admin: ver §RN-01).
5. `AuditLog` gravado: `Acao = "admin.servico.status_alterado"`.

### FP-05 — Moderar Imagens de Portfólio (AD-08)

1. Admin acessa fila de imagens pendentes de moderação (`Aprovada IS NULL`).
2. Admin visualiza cada imagem.
3. Admin aprova (`Aprovada = true`) ou rejeita (`Aprovada = false`).
4. Imagem rejeitada: arquivo removido do Cloudinary (Job SY-06).
5. `AuditLog` gravado: `Acao = "admin.imagem.aprovada"` ou `"admin.imagem.rejeitada"`.

> **Nota**: A moderação de imagens é automática via Cloudinary em condições normais. A fila manual é um fallback para imagens com score limítrofe ou falha no processamento automático.

### FP-06 — Listar Cobranças e Extrato Financeiro (AD-07 / AD-10)

1. Admin acessa a seção financeira.
2. Sistema retorna lista paginada de cobranças com status e valores.
3. Sistema exibe separadamente: receita total, taxa da plataforma acumulada e valor repassado a Prestadores.
4. Admin pode visualizar detalhes de cada cobrança (PIX, timestamps).

### FP-07 — Resolver Disputa (AD-11)

Ver RF-04, FP-08 — fluxo completo de resolução de disputa.

### FP-08 — Ler e Participar do Chat (AD-05 / AD-06)

Ver RF-06, FP-05 — Admin lê e pode enviar mensagens em qualquer serviço.

### FP-09 — Consultar Trilha de Auditoria (AD-12)

1. Admin acessa a seção de auditoria.
2. Admin filtra por: `Acao`, `Entidade`, `UsuarioId`, intervalo de datas.
3. Sistema retorna lista paginada de `AuditLog` com todos os campos.
4. Registros de `AuditLog` **nunca são deletados**.

---

## Fluxos Alternativos

### FA-01 — Acesso ao painel sem `Papel = Admin`

- **Ponto de desvio**: Qualquer requisição a `/api/admin/*` com token de usuário comum.
- **Ação**: `403 Forbidden` (middleware de autorização `[Authorize(Roles = "admin")]`).
- **Continuação**: Frontend redireciona para `/entrar` ou exibe tela de acesso negado.

---

## Fluxos de Exceção

### FE-01 — Cache de estatísticas expirado durante pico

- **Ponto de desvio**: TTL de 5 minutos expirado; query ao banco pode ser lenta.
- **Ação**: Query executada normalmente; cache renovado após retorno.
- **Continuação**: Resposta pode ter latência ligeiramente maior durante renovação.

---

## Pós-condições

- Todas as ações críticas do Admin registradas em `AuditLog`.
- Estados de usuários, serviços e cobranças refletidos em tempo real (respeitando TTL de cache de 5 min para estatísticas).

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | Acesso ao painel exclusivo para usuários com `Papel = Admin` — controlado pelo middleware `[Authorize(Roles = "admin")]`. **Não é baseado em e-mail específico.** |
| RN-02 | Toda ação do Admin gera `AuditLog` obrigatório com `Acao` prefixada com `"admin."`. |
| RN-03 | O Admin pode alterar o status de serviços para `Cancelado` a partir de qualquer estado ativo. Para demais transições, apenas as permitidas pela máquina de estados (RF-04). |
| RN-04 | O Admin pode revogar todas as sessões (Refresh Tokens) de um usuário via endpoint dedicado. |
| RN-05 | Registros de `AuditLog` são **imutáveis e nunca deletados** — apenas lidos. |
| RN-06 | Estatísticas do admin são cacheadas em memória por **5 minutos** (`IMemoryCache`). |
| RN-07 | Dados bancários do Prestador são acessíveis pelo Admin mas **nunca exibidos publicamente**. |
| RN-08 | Admin não pode ser bloqueado por outro Admin via painel (proteção de lockout). |
| RN-09 | Filtros de usuários aceitam `TipoConta`, `Especialidade` (texto livre, legacy) e `CidadeId` — não e-mail por questões de privacidade (LGPD). |

---

## Eventos de Domínio

O painel administrativo **consome** eventos mas não os publica diretamente. As ações do Admin que geram eventos são documentadas nos RFs correspondentes (RF-04 para serviços, RF-05 para cobranças, RF-06 para chat).

---

## Entidades Envolvidas

### `AuditLog`

**Tabela**: `logs_auditoria`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid?` | `user_id` | FK → `usuarios.id`; nulo para jobs |
| `Acao` | `string` | `action` | Ex: `admin.usuario.bloqueado` |
| `Entidade` | `string` | `entity` | Ex: `Usuario`, `Servico` |
| `EntidadeId` | `string?` | `entity_id` | ID da entidade afetada |
| `Ip` | `string?` | `ip_address` | IP da requisição |
| `UserAgent` | `string?` | `user_agent` | Header User-Agent |
| `Detalhes` | `string?` | `details` | JSON com contexto |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

**Índices**:
- `(user_id, created_at)` — auditoria por usuário
- `(entity, entity_id)` — auditoria por entidade
- `(created_at)` — purge e listagem cronológica

### `Disputa`

Ver RF-04 — entidade completa documentada lá.

### `Notificacao`

**Tabela**: `notificacoes`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `user_id` | FK → `usuarios.id` |
| `Titulo` | `string` | `title` | |
| `Mensagem` | `string` | `message` | |
| `Lida` | `bool` | `read` | Default `false` |
| `Tipo` | `string` | `type` | Ex: `disputa`, `pagamento` |
| `ReferenciaId` | `string?` | `reference_id` | ID da entidade relacionada |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

---

## API Endpoints

| Método | Rota | Auth | Caso de Uso |
|--------|------|------|-------------|
| `GET` | `/api/admin/stats` | Bearer (Admin) | AD-01 — Estatísticas |
| `GET` | `/api/admin/users` | Bearer (Admin) | AD-02 — Listar usuários |
| `GET` | `/api/admin/users/{id}` | Bearer (Admin) | AD-02 — Detalhe do usuário |
| `POST` | `/api/admin/users/{id}/bloquear` | Bearer (Admin) | AD-09 — Bloquear usuário |
| `POST` | `/api/admin/users/{id}/desbloquear` | Bearer (Admin) | AD-09 — Desbloquear usuário |
| `POST` | `/api/admin/users/{id}/revogar-sessoes` | Bearer (Admin) | Revogar Refresh Tokens |
| `GET` | `/api/admin/services` | Bearer (Admin) | AD-03 — Listar serviços |
| `PATCH` | `/api/admin/services/{id}` | Bearer (Admin) | AD-04 — Alterar status |
| `GET` | `/api/admin/services/{id}/messages` | Bearer (Admin) | AD-05 — Chat de auditoria |
| `POST` | `/api/admin/services/{id}/messages` | Bearer (Admin) | AD-06 — Enviar mensagem |
| `GET` | `/api/admin/charges` | Bearer (Admin) | AD-07 — Listar cobranças |
| `GET` | `/api/admin/financeiro` | Bearer (Admin) | AD-10 — Extrato financeiro |
| `GET` | `/api/admin/imagens/pendentes` | Bearer (Admin) | AD-08 — Imagens pendentes |
| `PATCH` | `/api/admin/imagens/{id}/moderar` | Bearer (Admin) | AD-08 — Aprovar/rejeitar |
| `GET` | `/api/admin/disputas` | Bearer (Admin) | AD-11 — Listar disputas |
| `PATCH` | `/api/admin/disputas/{id}/resolver` | Bearer (Admin) | AD-11 — Resolver disputa |
| `GET` | `/api/admin/audit-logs` | Bearer (Admin) | AD-12 — Trilha de auditoria |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Acesso a `/api/admin/*` com `Papel = Usuario` retorna `403 Forbidden`. |
| CA-02 | Dashboard exibe métricas corretas de usuários, serviços e financeiro. |
| CA-03 | Filtros de usuários por tipo, especialidade e cidade funcionam. |
| CA-04 | Bloquear usuário revoga todos os seus Refresh Tokens. |
| CA-05 | Toda ação do Admin gera `AuditLog` com `Acao` prefixada por `"admin."`. |
| CA-06 | Admin visualiza histórico completo de qualquer chat. |
| CA-07 | Imagens pendentes de moderação exibidas na fila; aprovação/rejeição funcional. |
| CA-08 | Extrato financeiro exibe receita total e taxa da plataforma separadamente. |
| CA-09 | Trilha de auditoria retorna registros com filtros funcionando. |
| CA-10 | Registros de `AuditLog` não podem ser deletados via API. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | GET `/api/admin/stats` com token Admin | `200 OK`, estatísticas retornadas |
| CT-02 | GET `/api/admin/stats` com token de Cliente | `403 Forbidden` |
| CT-03 | Admin filtra usuários por `TipoConta = Prestador` | Lista somente Prestadores |
| CT-04 | Admin bloqueia usuário | `DeletadoEm` preenchido, Refresh Tokens revogados, `AuditLog` gravado |
| CT-05 | Admin altera status de serviço para `Cancelado` | Status atualizado, `AuditLog` gravado |
| CT-06 | Admin acessa chat de serviço qualquer | `200 OK`, histórico completo |
| CT-07 | Admin resolve disputa em favor do Prestador | Pagamento liberado, `AuditLog` gravado |
| CT-08 | Admin aprova imagem pendente | `Aprovada = true`, imagem exibida no perfil |
| CT-09 | Admin consulta audit log com filtro de ação | Registros filtrados retornados |
| CT-10 | Estatísticas retornadas do cache em < 5 min | Segunda requisição sem query ao banco |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Acesso restrito ao e-mail `admin@prontto.com` | RF-07 PDF | Incorreto. Arquitetura usa `[Authorize(Roles = "admin")]` baseado em claim JWT — não e-mail específico. |
| Controle de aprovação de cadastros (automático/manual) | RF-07 PDF | Não documentado na arquitetura. Cadastros são aprovados automaticamente. Exige novo ADR para implementar fluxo de revisão. |
| Integração de notificação via WhatsApp e e-mail | RF-07 PDF | Mencionado como "futuramente" no PDF. Não documentado na arquitetura V1. |
| Dashboard "em tempo real" puro (sem cache) | RF-07 PDF | Arquitetura define cache de 5 minutos para estatísticas do admin. |
