# RF-04 — Solicitação e Gestão de Serviços

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Gerenciar o ciclo de vida completo de um serviço: desde a criação da solicitação pelo Cliente, passando pela negociação de preço, pagamento, execução, confirmação de conclusão, até eventual disputa — com rastreabilidade total de estados e atores.

---

## Descrição

O serviço (`Servico`) é o **agregado central** da plataforma. Seu ciclo de vida é governado por uma máquina de estados formal. O fluxo inicia quando o Cliente cria uma solicitação. Um Prestador se vincula e inicia a negociação de preço via mensagens do tipo `Proposta`. Após acordo, o pagamento é gerado (RF-05). Com o pagamento confirmado, o serviço avança para execução. O Prestador marca como concluído; o Cliente confirma ou abre disputa. Sem resposta em 7 dias, o sistema conclui automaticamente (Job SY-01).

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente | Cria a solicitação, negocia, paga, confirma ou contesta a conclusão |
| Prestador | Vincula-se à solicitação, negocia, executa, marca como concluído |
| Admin | Altera status manualmente, resolve disputas, cancela serviços |
| Sistema | Gerencia transições automáticas (conclusão automática, expiração de PIX) |

---

## Pré-condições

- Cliente autenticado (`TipoConta = Cliente`) para criar solicitação.
- Prestador autenticado (`TipoConta = Prestador`) com perfil completo para se vincular.
- Serviço deve estar no estado correto para cada transição.

---

## Fluxos Principais

### FP-01 — Criar Solicitação de Serviço (CL-06)

1. Cliente autenticado acessa o perfil de um Prestador ou a tela de serviços.
2. Cliente preenche: título, descrição (opcional), `CategoriaId`, `CidadeId`, endereço, data/hora desejada (`AgendadoEm`).
3. Sistema valida os dados via `FluentValidation`.
4. Sistema cria `Servico` com:
   - `ClienteId = cliente.Id`
   - `PrestadorId = null` (sem Prestador vinculado)
   - `Status = EmNegociacao`
   - `Preco = 0` (a ser acordado)
5. Sistema publica evento `SolicitacaoCriada`.
6. Sistema grava `AuditLog` com `Acao = "servico.criado"`.
7. Notificação enviada ao Prestador (se o Cliente acessou a partir de um perfil específico) via evento `SolicitacaoCriada`.

### FP-02 — Prestador Vincula-se à Solicitação (PR-06)

1. Prestador acessa lista de solicitações disponíveis (filtradas por suas categorias e cidades).
2. Prestador seleciona uma solicitação em `EmNegociacao` com `PrestadorId IS NULL`.
3. Sistema vincula: `Servico.PrestadorId = prestador.Id`.
4. Sistema publica evento `PrestadorVinculado`.
5. Notificação enviada ao Cliente.

### FP-03 — Negociação de Preço

A negociação ocorre via troca de `MensagemServico` com `TipoMensagem = Proposta`. O chat de texto coexiste no mesmo stream (ADR-04).

**Prestador envia proposta (PR-07)**:
1. Prestador envia mensagem com `TipoMensagem = Proposta` e `ValorProposta`.
2. Sistema cria `MensagemServico` com `StatusProposta = Pendente`.
3. Proposta anterior (se existir com `StatusProposta = Pendente`) é marcada como `Expirada`.
4. Sistema publica evento `PropostaEnviada`.
5. Notificação enviada ao Cliente.

**Cliente aceita proposta (CL-08)**:
1. Cliente aceita a proposta com `StatusProposta = Pendente`.
2. Sistema marca `StatusProposta = Aceita`.
3. Sistema atualiza `Servico.Preco = ValorProposta`.
4. Sistema publica evento `PropostaAceita` e `AcordoFirmado`.
5. Sistema avança `Servico.Status = AguardandoPagamento`.
6. Sistema cria `Cobranca` e gera PIX (RF-05, FP-01).

**Cliente contrapropoõe (CL-07)**:
1. Cliente cria nova mensagem com `TipoMensagem = Proposta` e `ValorProposta` diferente.
2. Proposta anterior do Prestador é marcada como `Expirada`.
3. Nova proposta criada com `StatusProposta = Pendente`.
4. Notificação enviada ao Prestador.

**Prestador aceita proposta do Cliente (PR-09)**: fluxo simétrico ao CL-08.

### FP-04 — Prestador Marca Serviço como Concluído (PR-10)

1. Serviço em `EmAndamento`.
2. Prestador aciona "Marcar como Concluído".
3. Sistema atualiza `Servico.Status = AguardandoConfirmacaoCliente`.
4. Sistema registra `AguardandoConfirmacaoDesde = UtcNow`.
5. Sistema publica evento `ServicoMarcadoConcluido`.
6. Notificação enviada ao Cliente.

### FP-05 — Cliente Confirma Conclusão (CL-10)

1. Serviço em `AguardandoConfirmacaoCliente`.
2. Cliente confirma a conclusão.
3. Sistema atualiza `Servico.Status = Concluido`, `ConcluidoEm = UtcNow`.
4. Sistema publica evento `ServicoConfirmadoCliente` e `ServicoConcluido`.
5. Sistema libera pagamento (RF-05, FP-03).
6. Notificação enviada ao Prestador.
7. Avaliação fica disponível para ambas as partes (RF-08).

### FP-06 — Auto-conclusão por Inatividade (SY-01)

1. Job `JobConclusaoAutomatica` executa a cada 1 hora.
2. Busca serviços com `Status = AguardandoConfirmacaoCliente` e `AguardandoConfirmacaoDesde < UtcNow - 7 dias`.
3. Para cada serviço encontrado:
   - `Servico.Status = Concluido`, `ConcluidoEm = UtcNow`.
   - Publica `ServicoConcluido_AutomaticoPorInatividade`.
   - Libera pagamento (RF-05).
   - Notifica Cliente e Prestador.
4. `AuditLog` registrado com `UsuarioId = null`, `Acao = "job.servico.auto_concluido"`.

### FP-07 — Cliente Abre Disputa (CL-17)

1. Serviço em `AguardandoConfirmacaoCliente`.
2. Cliente aciona "Contestar Conclusão" com motivo e descrição opcional.
3. Sistema cria entidade `Disputa` com `Status = Aberta`, `AbertaPorId = cliente.Id`.
4. Sistema atualiza `Servico.Status = EmDisputa`.
5. Cobrança permanece `Retido` — pagamento **não** é liberado.
6. Sistema publica evento `DisputaAberta`.
7. Notificação enviada ao Admin e ao Prestador.

### FP-08 — Admin Resolve Disputa (AD-11)

1. Serviço em `EmDisputa`.
2. Admin acessa painel de disputas e analisa a situação.
3. Admin seleciona decisão e registra `DecisaoAdmin` (justificativa obrigatória).

**Decisão em favor do Prestador**:
- `Servico.Status = Concluido`
- `Cobranca.Status = Liberado` — split 80/20 executado, repasse ao Prestador
- `Disputa.Status = ResolvidaPrestador`, `ResolvidoEm = UtcNow`, `ResolvidaPorId = admin.Id`

**Decisão em favor do Cliente**:
- `Servico.Status = Concluido`
- `Cobranca.Status = Reembolsado` — 100% devolvido ao Cliente
- `Disputa.Status = ResolvidaCliente`, `ResolvidoEm = UtcNow`, `ResolvidaPorId = admin.Id`

4. Sistema publica evento `DisputaResolvida`.
5. `AuditLog` obrigatório: `Acao = "disputa.resolvida"`.
6. Notificações enviadas ao Cliente e Prestador.

### FP-09 — Cancelamento de Serviço

- **De `EmNegociacao`**: Qualquer parte ou Admin pode cancelar. `Status = Cancelado`. Cobrança não foi criada.
- **De `AguardandoPagamento`**: Admin pode cancelar. PIX expirado (Job SY-02) também cancela.
- **De `EmAndamento`**: Apenas Admin pode cancelar. `Cobranca.Status = Reembolsado` se já `Pago`.
- **De `AguardandoConfirmacaoCliente`**: Admin pode cancelar (casos excepcionais).

---

## Fluxos Alternativos

### FA-01 — Prestador recusa proposta

- **Ponto de desvio**: Prestador recebe contraproposta e não aceita.
- **Ação**: Prestador envia nova proposta ou cancela o serviço.
- **Continuação**: Ciclo de negociação continua.

### FA-02 — Nenhum Prestador disponível

- **Ponto de desvio**: FP-01 — Cliente cria solicitação sem especificar Prestador.
- **Ação**: Solicitação fica disponível para Prestadores se vincularem (PR-05, PR-06).
- **Continuação**: Solicitação em `EmNegociacao` com `PrestadorId = null`.

---

## Fluxos de Exceção

### FE-01 — Transição de estado inválida

- **Ponto de desvio**: Ator tenta transição não permitida pela máquina de estados.
- **Ação**: Sistema retorna `422 Unprocessable Entity` com mensagem indicando o estado atual e transições válidas.

### FE-02 — Ator não autorizado para a operação

- **Ponto de desvio**: Ex: Prestador tenta confirmar conclusão (ação do Cliente).
- **Ação**: Sistema retorna `403 Forbidden`.

### FE-03 — Serviço em `EmDisputa` — operação bloqueada

- **Ponto de desvio**: Qualquer das partes tenta alterar o serviço enquanto em `EmDisputa`.
- **Ação**: Sistema retorna `422 Unprocessable Entity` — apenas Admin pode agir.

---

## Pós-condições

- `Servico` persistido com estado e timestamps corretos.
- `AuditLog` gravado em cada transição crítica.
- Eventos de domínio publicados para cada transição.
- Notificações enviadas aos atores relevantes.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | A máquina de estados do serviço define transições válidas. Transições inválidas são rejeitadas com `422`. Ciclo: `EmNegociacao → AguardandoPagamento → Pago → EmAndamento → AguardandoConfirmacaoCliente → Concluido`. |
| RN-02 | Somente a proposta mais recente pode ter `StatusProposta = Pendente`. Ao criar nova proposta, a anterior é marcada `Expirada`. |
| RN-03 | Um serviço pode ter no máximo **uma** disputa ativa. |
| RN-04 | Somente o **Cliente** pode abrir disputa, e apenas no estado `AguardandoConfirmacaoCliente`. |
| RN-05 | Enquanto em `EmDisputa`: nenhuma das partes (exceto Admin) pode alterar o serviço. |
| RN-06 | Auto-conclusão ocorre após **7 dias** em `AguardandoConfirmacaoCliente` sem ação do Cliente (Job SY-01, frequência: 1 hora). |
| RN-07 | `TaxaAdminRate` padrão é `0.2000` (20%). O valor final ao Prestador é `Preco * 0.8`. |
| RN-08 | `CategoriaId` e `CidadeId` no serviço são FKs para entidades canônicas — nunca string livre. |
| RN-09 | O Admin pode cancelar serviços em qualquer estado (exceto `Concluido`). |
| RN-10 | `AuditLog` é obrigatório para: criação de serviço, cancelamento, abertura de disputa, resolução de disputa. |
| RN-11 | **Pagamento NÃO é coletado na solicitação.** É gerado após acordo de preço, quando o serviço avança para `AguardandoPagamento`. |

---

## Máquina de Estados — `StatusServico`

| De | Para | Gatilho | Ator |
|----|------|---------|------|
| `EmNegociacao` | `AguardandoPagamento` | Proposta aceita por ambas as partes | Sistema |
| `EmNegociacao` | `Cancelado` | Cancelamento | Cliente / Prestador / Admin |
| `AguardandoPagamento` | `Pago` | Webhook PIX confirmado (Pagar.me) | Sistema (webhook) |
| `AguardandoPagamento` | `Cancelado` | PIX expirado sem pagamento | Sistema (job SY-02) |
| `Pago` | `EmAndamento` | Imediato após pagamento confirmado | Sistema |
| `EmAndamento` | `AguardandoConfirmacaoCliente` | Prestador marca concluído | Prestador |
| `EmAndamento` | `Cancelado` | Admin | Admin |
| `AguardandoConfirmacaoCliente` | `Concluido` | Cliente confirma | Cliente |
| `AguardandoConfirmacaoCliente` | `Concluido` | 7 dias sem resposta | Sistema (Job SY-01) |
| `AguardandoConfirmacaoCliente` | `EmDisputa` | Cliente abre disputa | Cliente |
| `AguardandoConfirmacaoCliente` | `Cancelado` | Admin (casos excepcionais) | Admin |
| `EmDisputa` | `Concluido` | Admin decide em favor do Prestador | Admin |
| `EmDisputa` | `Concluido` | Admin decide em favor do Cliente (com reembolso) | Admin |
| `EmDisputa` | `Cancelado` | Admin (casos excepcionais) | Admin |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `SolicitacaoCriada` | Serviço criado | `{ ServicoId, ClienteId, Categoria, Cidade }` |
| `PrestadorVinculado` | Prestador se vincula | `{ ServicoId, PrestadorId }` |
| `PropostaEnviada` | Proposta enviada | `{ ServicoId, MensagemId, Valor, PapelRemetente }` |
| `PropostaAceita` | Proposta aceita | `{ ServicoId, MensagemId, ValorAcordado }` |
| `AcordoFirmado` | Acordo de preço firmado | `{ ServicoId, ValorFinal, ClienteId, PrestadorId }` |
| `ServicoEmAndamento` | Pagamento confirmado, serviço inicia | `{ ServicoId, ClienteId, PrestadorId }` |
| `ServicoMarcadoConcluido` | Prestador marca concluído | `{ ServicoId, PrestadorId, MarcadoEm }` |
| `ServicoConfirmadoCliente` | Cliente confirma conclusão | `{ ServicoId, ClienteId, ConfirmadoEm }` |
| `ServicoConcluido` | Serviço concluído (qualquer via) | `{ ServicoId, ClienteId, PrestadorId, ValorPrestador }` |
| `ServicoConcluido_AutomaticoPorInatividade` | Auto-conclusão por job | `{ ServicoId, ConcluidoEm }` |
| `ServicoCancelado` | Serviço cancelado | `{ ServicoId, MotivoCancelamento, AtorId }` |
| `DisputaAberta` | Disputa criada pelo Cliente | `{ DisputaId, ServicoId, ClienteId, Motivo }` |
| `DisputaEmAnalise` | Admin inicia análise | `{ DisputaId, AdminId }` |
| `DisputaResolvida` | Admin resolve disputa | `{ DisputaId, ServicoId, Resultado, AdminId }` |

---

## Entidades Envolvidas

### `Servico`

**Tabela**: `servicos`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Titulo` | `string` | `title` | Obrigatório |
| `Descricao` | `string?` | `description` | Opcional |
| `CategoriaId` | `Guid` | `category_id` | FK → `categorias.id` |
| `CidadeId` | `Guid?` | `city_id` | FK → `cidades.id` |
| `ClienteId` | `Guid?` | `client_id` | FK → `usuarios.id` |
| `PrestadorId` | `Guid?` | `provider_id` | FK → `usuarios.id`; null até vinculação |
| `Preco` | `decimal` | `price` | Valor acordado na negociação |
| `TaxaAdminRate` | `decimal` | `admin_fee_rate` | Padrão `0.2000` |
| `Status` | `StatusServico` | `status` | Máquina de estados |
| `Endereco` | `string?` | `address` | Endereço de execução |
| `AgendadoEm` | `DateTime?` | `scheduled_at` | UTC |
| `ConcluidoEm` | `DateTime?` | `completed_at` | UTC |
| `AguardandoConfirmacaoDesde` | `DateTime?` | `awaiting_confirmation_since` | Base para auto-conclusão |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `AtualizadoEm` | `DateTime` | `updated_at` | UTC |
| `DeletadoEm` | `DateTime?` | `deleted_at` | Soft delete |

### `MensagemServico` (propostas)

**Tabela**: `mensagens_servico`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `service_id` | FK → `servicos.id` |
| `RemetenteId` | `Guid?` | `sender_id` | FK → `usuarios.id` |
| `PapelRemetente` | `PapelRemetente` | `sender_role` | `cliente`/`prestador`/`admin`/`sistema` |
| `TipoMensagem` | `TipoMensagem` | `message_type` | `texto`/`imagem`/`proposta`/`sistema` |
| `Conteudo` | `string` | `content` | Texto da mensagem |
| `ValorProposta` | `decimal?` | `proposal_amount` | Preenchido quando `TipoMensagem = Proposta` |
| `StatusProposta` | `StatusProposta?` | `proposal_status` | `pendente`/`aceita`/`recusada`/`expirada` |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

### `Disputa`

**Tabela**: `disputas`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `service_id` | FK único → `servicos.id` |
| `AbertaPorId` | `Guid` | `opened_by_id` | FK → `usuarios.id`; deve ser o Cliente |
| `Motivo` | `string` | `reason` | Motivo selecionado |
| `Descricao` | `string?` | `description` | Detalhamento opcional |
| `Status` | `StatusDisputa` | `status` | `Aberta`/`EmAnalise`/`ResolvidaCliente`/`ResolvidaPrestador` |
| `ResolvidaPorId` | `Guid?` | `resolved_by_id` | FK → `usuarios.id`; Admin |
| `DecisaoAdmin` | `string?` | `admin_decision` | Justificativa obrigatória ao resolver |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `ResolvidoEm` | `DateTime?` | `resolved_at` | UTC |

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/servicos` | Bearer (Cliente) | Criar solicitação |
| `GET` | `/api/servicos` | Bearer | Listar serviços do usuário |
| `GET` | `/api/servicos/{id}` | Bearer | Detalhe do serviço |
| `POST` | `/api/servicos/{id}/vincular` | Bearer (Prestador) | Prestador se vincula |
| `POST` | `/api/servicos/{id}/proposta` | Bearer | Enviar proposta de preço |
| `PATCH` | `/api/servicos/{id}/proposta/{mensagemId}/aceitar` | Bearer | Aceitar proposta |
| `PATCH` | `/api/servicos/{id}/concluir` | Bearer (Prestador) | Marcar como concluído |
| `PATCH` | `/api/servicos/{id}/confirmar` | Bearer (Cliente) | Confirmar conclusão |
| `PATCH` | `/api/servicos/{id}/cancelar` | Bearer | Cancelar serviço |
| `POST` | `/api/servicos/{id}/disputa` | Bearer (Cliente) | Abrir disputa |
| `PATCH` | `/api/admin/services/{id}` | Bearer (Admin) | Alterar status manualmente |
| `PATCH` | `/api/admin/disputas/{id}/resolver` | Bearer (Admin) | Resolver disputa |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Solicitação criada com `Status = EmNegociacao` e `PrestadorId = null`. |
| CA-02 | Transições de estado inválidas retornam `422`. |
| CA-03 | Proposta aceita avança serviço para `AguardandoPagamento` e cria `Cobranca`. |
| CA-04 | Somente a proposta mais recente tem `StatusProposta = Pendente`. |
| CA-05 | Auto-conclusão executa após 7 dias sem confirmação do Cliente. |
| CA-06 | Somente o Cliente pode abrir disputa, no estado `AguardandoConfirmacaoCliente`. |
| CA-07 | Em `EmDisputa`, nenhuma das partes pode alterar o serviço (apenas Admin). |
| CA-08 | Resolução de disputa registra `DecisaoAdmin` e grava `AuditLog`. |
| CA-09 | Pagamento **não** é coletado na criação da solicitação. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Cliente cria solicitação válida | `201 Created`, `Status = EmNegociacao` |
| CT-02 | Prestador se vincula à solicitação | `PrestadorId` preenchido |
| CT-03 | Prestador envia proposta | Mensagem criada com `TipoMensagem = Proposta`, `StatusProposta = Pendente` |
| CT-04 | Cliente aceita proposta | `Servico.Status = AguardandoPagamento`, `Cobranca` criada |
| CT-05 | Prestador tenta aceitar própria proposta | `403 Forbidden` |
| CT-06 | Prestador marca serviço concluído | `Status = AguardandoConfirmacaoCliente`, notificação ao Cliente |
| CT-07 | Cliente confirma conclusão | `Status = Concluido`, pagamento liberado |
| CT-08 | Cliente abre disputa | `Status = EmDisputa`, `Cobranca` permanece `Retido` |
| CT-09 | Admin resolve disputa em favor do Prestador | `Cobranca = Liberado`, `AuditLog` gravado |
| CT-10 | Admin resolve disputa em favor do Cliente | `Cobranca = Reembolsado`, `AuditLog` gravado |
| CT-11 | Transição inválida (`Concluido → EmAndamento`) | `422 Unprocessable Entity` |
| CT-12 | Serviço em `EmDisputa`, Cliente tenta alterar | `422 Unprocessable Entity` |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Ciclo "solicitado → aceito → em andamento → concluído" (4 estados) | RF-04 PDF | Incorreto. A arquitetura possui 7+ estados com máquina formal: `EmNegociacao → AguardandoPagamento → Pago → EmAndamento → AguardandoConfirmacaoCliente → Concluido/EmDisputa/Cancelado`. |
| Pagamento coletado no momento da solicitação | RF-04 PDF | Incorreto. Pagamento ocorre após acordo de preço (`AguardandoPagamento`). |
| Notificação por e-mail | RF-04 PDF | Arquitetura documenta notificações in-app (`Notificacao`). Integração com e-mail externo não está documentada na V1. |
| "BD-03" como nome do banco de dados do serviço | RF-04 PDF | Tabela é `servicos` no PostgreSQL. "BD-03" é nomenclatura do PDF sem correlação com a arquitetura. |
