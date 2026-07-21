# RF-06 — Chat Interno

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Disponibilizar um canal de comunicação interno entre Cliente e Prestador vinculado a cada serviço, suportando mensagens de texto, imagens (com moderação) e propostas de preço — acessível também pelo Admin para auditoria.

---

## Descrição

O chat é implementado como uma sequência de `MensagemServico` vinculadas a um `Servico`. As mensagens suportam quatro tipos: `texto`, `imagem`, `proposta` e `sistema`. Propostas de preço (negociação) coexistem no mesmo stream do chat (ADR-04).

**Na V1**, o frontend atualiza o chat via **polling a cada 10 segundos** (§11.4 da arquitetura). WebSocket (SignalR) está planejado para V2. Imagens do chat passam por moderação automática via Cloudinary antes de serem exibidas. O histórico é paginado com cursor-based pagination (`afterId`), carregando os últimos 50 mensagens.

O Admin pode ler e enviar mensagens em qualquer serviço para fins de auditoria e mediação.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente | Participa do chat como contratante do serviço |
| Prestador | Participa do chat como executor do serviço |
| Admin | Lê e envia mensagens em qualquer serviço (AD-05, AD-06) |
| Sistema | Publica mensagens automáticas de mudança de estado (`TipoMensagem = Sistema`) |

---

## Pré-condições

- O serviço deve existir e estar em estado ativo (não `Cancelado`).
- O usuário deve ser participante do serviço (`ClienteId` ou `PrestadorId`) ou Admin.
- Para envio de imagens: Cloudinary configurado com moderação habilitada.

---

## Fluxos Principais

### FP-01 — Enviar Mensagem de Texto (CL-13 / PR-13)

1. Usuário autenticado (Cliente ou Prestador) acessa o chat de um serviço.
2. Usuário digita a mensagem e submete.
3. Sistema valida: usuário é participante do serviço; serviço não está `Cancelado`.
4. Sistema cria `MensagemServico`:
   - `TipoMensagem = Texto`
   - `Conteudo = mensagem do usuário` (sanitizado com `DomSanitizer` no Angular)
   - `PapelRemetente` definido conforme papel do usuário no serviço
   - `RemetenteId = usuário.Id`
5. Sistema retorna a mensagem criada.
6. Frontend atualiza a lista de mensagens (polling ou resposta imediata da requisição).

### FP-02 — Enviar Imagem no Chat (CL-14 / PR-14)

1. Usuário seleciona imagem (tipos aceitos: `image/jpeg`, `image/png`, `image/webp`; máximo 10 MB).
2. Frontend solicita URL assinada ao backend: `POST /api/servicos/{id}/chat/url-assinada`.
3. Backend valida participação no serviço e gera URL assinada via `IArmazenamentoArquivo.GerarUrlAssinadaAsync()`.
4. Frontend faz upload direto ao Cloudinary (ADR-03). Backend não recebe bytes.
5. Frontend envia a URL resultante ao backend: `POST /api/servicos/{id}/chat/mensagens`.
6. Sistema cria `MensagemServico`:
   - `TipoMensagem = Imagem`
   - `Conteudo = URL Cloudinary`
   - `ImagemModerada = false`
   - `ImagemAprovada = null`
7. Sistema publica evento `ImagemSubmetidaModeracao`.
8. Frontend exibe overlay de "aguardando moderação" sobre o espaço da imagem.
9. Cloudinary processa moderação automaticamente.
10. Callback chama `ServicoModeracao.ProcessarResultadoAsync()`:
    - Aprovada: `ImagemModerada = true`, `ImagemAprovada = true` — imagem exibida.
    - Rejeitada: `ImagemModerada = true`, `ImagemAprovada = false` — imagem removida do Cloudinary (Job SY-06); mensagem marcada como rejeitada.
11. Sistema publica `ImagemAprovada` ou `ImagemRejeitada`.

### FP-03 — Enviar Proposta de Preço (PR-07 / CL-07)

Ver RF-04, FP-03. Proposta é uma `MensagemServico` com `TipoMensagem = Proposta` e `ValorProposta`. A lógica de aceitação e expiração pertence ao domínio de negociação (RF-04).

### FP-04 — Carregar Histórico do Chat

1. Usuário (ou Admin) abre o chat de um serviço.
2. Frontend faz `GET /api/servicos/{id}/chat/mensagens?pageSize=50` (sem `afterId` para carregar as mais recentes).
3. Sistema retorna as últimas 50 mensagens ordenadas por `CriadoEm DESC`.
4. Usuário rola para cima → frontend faz nova requisição com `afterId = id da mensagem mais antiga carregada`.
5. Sistema retorna as mensagens anteriores (paginação cursor-based).

### FP-05 — Admin Lê e Envia Mensagens (AD-05 / AD-06)

1. Admin acessa o chat de qualquer serviço via painel administrativo.
2. Admin visualiza histórico completo (sem restrição de participação).
3. Admin pode enviar mensagem como plataforma:
   - `TipoMensagem = Texto`
   - `PapelRemetente = Admin`
   - `RemetenteId = admin.Id`
4. Todas as ações do Admin no chat são registradas em `AuditLog`.

### FP-06 — Polling de Novas Mensagens (V1)

1. Frontend detecta que o usuário está com o chat aberto.
2. A cada **10 segundos**, frontend faz `GET /api/servicos/{id}/chat/mensagens?afterId={ultimaMensagemId}`.
3. Se há novas mensagens, frontend as exibe em tempo real (percepção do usuário).
4. Se nenhuma mensagem nova, resposta retorna lista vazia.

---

## Fluxos Alternativos

### FA-01 — Serviço cancelado

- **Ponto de desvio**: FP-01, passo 3 — `Servico.Status = Cancelado`.
- **Ação**: Sistema retorna `422 Unprocessable Entity` — envio de mensagem bloqueado.
- **Continuação**: Usuário pode apenas ler o histórico.

### FA-02 — Imagem rejeitada pela moderação

- **Ponto de desvio**: FP-02, passo 10 — Cloudinary rejeita a imagem.
- **Ação**: `ImagemAprovada = false`; imagem não exibida; arquivo removido do Cloudinary.
- **Continuação**: Usuário vê indicação de que a imagem foi rejeitada.

### FA-03 — Usuário não participante tenta acessar chat

- **Ponto de desvio**: FP-01, passo 3 — `ClienteId != usuário.Id` e `PrestadorId != usuário.Id` e `Papel != Admin`.
- **Ação**: Sistema retorna `403 Forbidden`.

---

## Fluxos de Exceção

### FE-01 — Arquivo inválido para upload

- **Ponto de desvio**: FP-02, passo 1 — tipo ou tamanho inválido.
- **Ação**: `400 Bad Request` antes de gerar URL assinada.

### FE-02 — Rate limiting do chat atingido

- **Ponto de desvio**: Mais de 30 mensagens por minuto pelo mesmo usuário.
- **Ação**: `429 Too Many Requests`.

---

## Pós-condições

- Mensagem de texto persistida e disponível via polling no próximo ciclo de 10s.
- Imagem pendente de moderação exibida com overlay; exibida após aprovação.
- `AuditLog` gravado para ações do Admin no chat.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | O chat é vinculado a um `Servico`. Não existe chat avulso entre usuários — somente no contexto de um serviço. |
| RN-02 | Somente participantes do serviço (`ClienteId`, `PrestadorId`) e usuários com `Papel = Admin` podem ler e escrever no chat. |
| RN-03 | **V1**: atualização via **polling a cada 10 segundos** no Angular. WebSocket (SignalR) está planejado para V2. |
| RN-04 | Imagens do chat não são exibidas até `ImagemAprovada = true`. Frontend exibe overlay durante moderação. |
| RN-05 | Upload de imagem segue o padrão URL assinada (ADR-03): browser faz upload direto ao Cloudinary. Backend não recebe bytes. |
| RN-06 | Tamanho máximo de imagem: **10 MB**. Tipos aceitos: `image/jpeg`, `image/png`, `image/webp`. |
| RN-07 | Paginação do histórico: **50 mensagens** por página, cursor-based (`afterId`). |
| RN-08 | Mensagens automáticas de sistema (ex: "Serviço concluído", "Proposta aceita") têm `TipoMensagem = Sistema` e `RemetenteId = null`. |
| RN-09 | Conteúdo de mensagens de texto deve ser sanitizado no Angular com `DomSanitizer` antes da renderização (proteção contra XSS). |
| RN-10 | Rate limiting: máximo **30 mensagens/minuto** por usuário no endpoint de chat. |
| RN-11 | Serviços em `EmDisputa` permitem leitura do chat mas o Admin é o único que pode enviar mensagens (outros participantes são bloqueados). |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `ImagemSubmetidaModeracao` | Imagem enviada ao chat | `{ Referencia: MensagemId, TipoReferencia: "chat", CloudinaryPublicId }` |
| `ImagemAprovada` | Moderação aprova imagem do chat | `{ CloudinaryPublicId, Referencia: MensagemId }` |
| `ImagemRejeitada` | Moderação rejeita imagem do chat | `{ CloudinaryPublicId, Referencia: MensagemId }` |

---

## Entidades Envolvidas

### `MensagemServico`

**Tabela**: `mensagens_servico`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `service_id` | FK → `servicos.id` |
| `RemetenteId` | `Guid?` | `sender_id` | FK → `usuarios.id`; null para msgs de sistema |
| `PapelRemetente` | `PapelRemetente` | `sender_role` | `cliente`/`prestador`/`admin`/`sistema` |
| `TipoMensagem` | `TipoMensagem` | `message_type` | `texto`/`imagem`/`proposta`/`sistema` |
| `Conteudo` | `string` | `content` | Texto ou URL Cloudinary moderada |
| `ValorProposta` | `decimal?` | `proposal_amount` | Somente para `TipoMensagem = Proposta` |
| `StatusProposta` | `StatusProposta?` | `proposal_status` | `pendente`/`aceita`/`recusada`/`expirada` |
| `ImagemModerada` | `bool` | `image_moderated` | True após análise Cloudinary |
| `ImagemAprovada` | `bool?` | `image_approved` | `null` = pendente; `true` = ok; `false` = rejeitada |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

**Índice**: `(service_id, created_at)` composto — paginação eficiente do chat.

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/servicos/{id}/chat/mensagens` | Bearer (participante) | Histórico paginado (`pageSize`, `afterId`) |
| `POST` | `/api/servicos/{id}/chat/mensagens` | Bearer (participante) | Enviar mensagem de texto ou URL de imagem |
| `POST` | `/api/servicos/{id}/chat/url-assinada` | Bearer (participante) | Gerar URL assinada para upload de imagem |
| `GET` | `/api/admin/services/{id}/messages` | Bearer (Admin) | Histórico completo (auditoria) |
| `POST` | `/api/admin/services/{id}/messages` | Bearer (Admin) | Enviar mensagem como plataforma |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Mensagens de texto enviadas por participantes são persistidas e retornadas no próximo poll. |
| CA-02 | Usuário não participante recebe `403 Forbidden`. |
| CA-03 | Imagem enviada fica com overlay até `ImagemAprovada = true`. |
| CA-04 | Imagem rejeitada não é exibida e arquivo é removido do Cloudinary. |
| CA-05 | Admin visualiza histórico completo de qualquer serviço. |
| CA-06 | Paginação cursor-based com `afterId` funciona corretamente. |
| CA-07 | Rate limiting de 30 msgs/min por usuário bloqueia com `429`. |
| CA-08 | Propostas no chat exibem `ValorProposta` e `StatusProposta` corretamente. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Cliente envia mensagem de texto | `201 Created`, mensagem retornada no próximo poll |
| CT-02 | Terceiro usuário (não participante) acessa chat | `403 Forbidden` |
| CT-03 | Upload de imagem válida | Criada com `ImagemAprovada = null`; exibida após aprovação |
| CT-04 | Moderação aprova imagem | `ImagemAprovada = true`, imagem exibida |
| CT-05 | Moderação rejeita imagem | `ImagemAprovada = false`, imagem não exibida |
| CT-06 | Admin lê chat de serviço alheio | `200 OK` |
| CT-07 | Admin envia mensagem | Mensagem criada com `PapelRemetente = Admin` |
| CT-08 | Scroll carrega mensagens anteriores via `afterId` | Mensagens mais antigas retornadas |
| CT-09 | 31ª mensagem no mesmo minuto | `429 Too Many Requests` |
| CT-10 | Mensagem em serviço `Cancelado` | `422 Unprocessable Entity` |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Chat em tempo real (WebSocket) na V1 | RF-06 PDF | V1 usa polling a cada 10 segundos (§11.4). WebSocket (SignalR) planejado para V2. |
| Bloqueio automático de números de telefone nas mensagens | RF-06 PDF | Não documentado na arquitetura. Exige novo ADR para definir estratégia de filtragem. |
| Bloqueio automático de palavrões | RF-06 PDF | Não documentado na arquitetura. Exige novo ADR e implementação de dicionário/serviço externo. |
| Chat disponível antes da criação da solicitação | RF-06 PDF | O chat é vinculado ao serviço; só existe após `SolicitacaoCriada`. |
