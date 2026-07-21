# RF-08 — Sistema de Avaliações

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Permitir a avaliação bilateral entre Cliente e Prestador após a conclusão de um serviço, calculando e exibindo médias de avaliação nos perfis públicos para gerar confiabilidade na plataforma.

---

## Descrição

A avaliação é **bilateral**: o Cliente avalia o Prestador, e o Prestador avalia o Cliente. Cada parte avalia de forma independente após a conclusão do serviço. A avaliação consiste em nota de 1 a 5 estrelas e comentário opcional.

A janela de avaliação é de **30 dias** após a conclusão. Após esse prazo, a avaliação não pode mais ser registrada. Cada pessoa pode avaliar somente uma vez por serviço (constraint `UNIQUE(service_id, reviewer_id)`).

A média de avaliações é recalculada automaticamente pelo Job `JobMediaAvaliacao` após cada nova avaliação registrada.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente | Avalia o Prestador após conclusão do serviço (CL-11) |
| Prestador | Avalia o Cliente após conclusão do serviço (PR-11) |
| Sistema | Notifica sobre conclusão; Job recalcula médias |

---

## Pré-condições

- O serviço deve estar com `Status = Concluido`.
- O avaliador deve ser participante do serviço (`ClienteId` ou `PrestadorId`).
- A avaliação deve ser criada dentro da **janela de 30 dias** após `ConcluidoEm`.
- O avaliador não deve ter registrado avaliação anterior para o mesmo serviço (`UNIQUE(service_id, reviewer_id)`).

---

## Fluxos Principais

### FP-01 — Cliente Avalia Prestador (CL-11)

1. Serviço transicionado para `Concluido`.
2. Sistema publica evento `ServicoConcluido`.
3. Sistema envia notificação ao Cliente: "Seu serviço foi concluído! Avalie o Prestador."
4. Cliente acessa o serviço concluído e clica em "Avaliar".
5. Sistema verifica pré-condições: `Status = Concluido`, janela de 30 dias, avaliação não existente.
6. Cliente preenche nota (1-5) e comentário opcional (máximo 1000 caracteres).
7. Sistema cria `Avaliacao`:
   - `AvaliadorId = cliente.Id`
   - `AvaliadoId = prestador.Id`
   - `ServicoId = servico.Id`
   - `Nota = nota`
   - `Comentario = comentario`
8. Sistema publica evento `AvaliacaoRegistrada`.
9. Job `JobMediaAvaliacao` recalcula `MediaAvaliacoes` e `TotalAvaliacoes` do Prestador.
10. Avaliação exibida no perfil público do Prestador imediatamente após persistência.

### FP-02 — Prestador Avalia Cliente (PR-11)

1. Serviço transicionado para `Concluido`.
2. Sistema envia notificação ao Prestador: "Avalie o Cliente do serviço concluído."
3. Prestador acessa o serviço e clica em "Avaliar".
4. Sistema verifica pré-condições (mesmas do FP-01).
5. Prestador preenche nota (1-5) e comentário opcional.
6. Sistema cria `Avaliacao`:
   - `AvaliadorId = prestador.Id`
   - `AvaliadoId = cliente.Id`
   - `ServicoId = servico.Id`
7. Sistema publica evento `AvaliacaoRegistrada`.
8. Job `JobMediaAvaliacao` recalcula `MediaAvaliacoes` e `TotalAvaliacoes` do Cliente.

### FP-03 — Recálculo de Média (Job SY-07 / JobMediaAvaliacao)

Gatilho: handler do evento `AvaliacaoRegistrada`.

1. Job executa `SELECT AVG(rating), COUNT(*) FROM avaliacoes WHERE reviewed_id = {UsuarioId}`.
2. Atualiza `Usuario.MediaAvaliacoes` e `Usuario.TotalAvaliacoes`.
3. Publica evento `MediaAvaliacaoAtualizada`.

### FP-04 — Visualização de Avaliações no Perfil Público

1. Visitante acessa perfil público do Prestador (RF-02, FP-01).
2. Sistema retorna avaliações onde `AvaliadoId = prestador.Id`, com paginação.
3. Frontend exibe: nota (estrelas), comentário e data (`CriadoEm`) de cada avaliação.
4. Frontend exibe média (`MediaAvaliacoes`) e total (`TotalAvaliacoes`) no topo do perfil e nos cards de listagem.

---

## Fluxos Alternativos

### FA-01 — Usuário tenta avaliar após 30 dias

- **Ponto de desvio**: FP-01 ou FP-02, passo 5 — `UtcNow > ConcluidoEm + 30 dias`.
- **Ação**: Sistema retorna `422 Unprocessable Entity` — "Prazo para avaliação expirado".
- **Continuação**: Usuário não pode mais avaliar esse serviço.

### FA-02 — Usuário tenta avaliar um serviço novamente

- **Ponto de desvio**: FP-01 ou FP-02, passo 5 — registro existente com `UNIQUE(service_id, reviewer_id)`.
- **Ação**: Sistema retorna `409 Conflict` — "Você já avaliou este serviço".
- **Continuação**: Usuário não pode sobrescrever a avaliação.

---

## Fluxos de Exceção

### FE-01 — Avaliação de serviço não concluído

- **Ponto de desvio**: FP-01, passo 5 — `Servico.Status != Concluido`.
- **Ação**: Sistema retorna `422 Unprocessable Entity`.

### FE-02 — Nota fora do intervalo

- **Ponto de desvio**: FP-01, passo 6 — nota < 1 ou > 5.
- **Ação**: `FluentValidation` retorna `400 Bad Request`.

### FE-03 — Comentário acima de 1000 caracteres

- **Ponto de desvio**: FP-01, passo 6 — `Comentario.Length > 1000`.
- **Ação**: `FluentValidation` retorna `400 Bad Request`.

---

## Pós-condições

- `Avaliacao` persistida com `AvaliadorId`, `AvaliadoId`, `ServicoId`, `Nota`, `Comentario`.
- `MediaAvaliacoes` e `TotalAvaliacoes` do usuário avaliado atualizados.
- Evento `AvaliacaoRegistrada` publicado.
- Notificação enviada ao usuário avaliado.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | A avaliação é **bilateral**: Cliente avalia Prestador (CL-11) **e** Prestador avalia Cliente (PR-11). São avaliações independentes — cada uma tem seu próprio registro `Avaliacao`. |
| RN-02 | Avaliação só pode ser feita com `Servico.Status = Concluido`. |
| RN-03 | **Janela de avaliação: 30 dias** após `Servico.ConcluidoEm`. Após esse prazo, `422 Unprocessable Entity`. |
| RN-04 | Constraint `UNIQUE(service_id, reviewer_id)` — cada participante pode avaliar **somente uma vez** por serviço. |
| RN-05 | Nota deve ser inteiro entre **1 e 5** (constraint check no banco). |
| RN-06 | Comentário é **opcional**, máximo 1000 caracteres. |
| RN-07 | A média (`MediaAvaliacoes`) é calculada por Job (`JobMediaAvaliacao`) como `AVG(Nota)` das avaliações onde o usuário é `AvaliadoId`. |
| RN-08 | Avaliações são **imutáveis** após criação — não há edição de avaliação. |
| RN-09 | Avaliações são exibidas **publicamente** no perfil do avaliado com nota, comentário e data. |
| RN-10 | O avaliador deve ser participante do serviço — `ClienteId` ou `PrestadorId`. |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `AvaliacaoRegistrada` | Avaliação persistida | `{ AvaliacaoId, ServicoId, AvaliadorId, AvaliadoId, Nota }` |
| `MediaAvaliacaoAtualizada` | Job recalcula média | `{ UsuarioId, NovaMedia, TotalAvaliacoes }` |

---

## Entidades Envolvidas

### `Avaliacao`

**Tabela**: `avaliacoes`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `service_id` | FK → `servicos.id` |
| `AvaliadorId` | `Guid` | `reviewer_id` | FK → `usuarios.id`; quem avalia |
| `AvaliadoId` | `Guid` | `reviewed_id` | FK → `usuarios.id`; quem recebe a avaliação |
| `Nota` | `int` | `rating` | 1 a 5 (constraint check no banco) |
| `Comentario` | `string?` | `comment` | Opcional; máximo 1000 chars |
| `CriadoEm` | `DateTime` | `created_at` | UTC |

**Constraint**: `UNIQUE(service_id, reviewer_id)` — uma avaliação por pessoa por serviço.

**Índices**:
- `reviewed_id` (B-tree) — avaliações recebidas (perfil público)
- `(service_id, reviewer_id)` (UNIQUE) — idempotência

### `Usuario` (campos relacionados)

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `MediaAvaliacoes` | `decimal` | `rating_average` | Calculado por `JobMediaAvaliacao` |
| `TotalAvaliacoes` | `int` | `rating_count` | Contagem atualizada pelo Job |

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/servicos/{id}/avaliacoes` | Bearer (participante) | Registrar avaliação |
| `GET` | `/api/prestadores/{slug}/avaliacoes` | Público | Avaliações recebidas pelo Prestador |
| `GET` | `/api/servicos/{id}/avaliacoes` | Bearer (participante) | Avaliações do serviço |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Cliente avalia Prestador após `Servico.Status = Concluido`. |
| CA-02 | Prestador avalia Cliente após `Servico.Status = Concluido`. |
| CA-03 | Segunda avaliação pelo mesmo avaliador para o mesmo serviço retorna `409 Conflict`. |
| CA-04 | Avaliação após 30 dias retorna `422 Unprocessable Entity`. |
| CA-05 | `MediaAvaliacoes` e `TotalAvaliacoes` atualizados após nova avaliação. |
| CA-06 | Avaliações exibidas no perfil público com nota, comentário e data. |
| CA-07 | Nota fora do intervalo 1-5 retorna `400 Bad Request`. |
| CA-08 | Avaliação por usuário não participante do serviço retorna `403 Forbidden`. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Cliente avalia Prestador em serviço `Concluido` | `201 Created`, `JobMediaAvaliacao` disparado |
| CT-02 | Prestador avalia Cliente em serviço `Concluido` | `201 Created`, média do Cliente atualizada |
| CT-03 | Cliente tenta avaliar serviço em `EmAndamento` | `422 Unprocessable Entity` |
| CT-04 | Cliente avalia o mesmo serviço duas vezes | `409 Conflict` |
| CT-05 | Avaliação 31 dias após `ConcluidoEm` | `422 Unprocessable Entity` |
| CT-06 | Nota = 0 | `400 Bad Request` |
| CT-07 | Nota = 6 | `400 Bad Request` |
| CT-08 | Comentário com 1001 caracteres | `400 Bad Request` |
| CT-09 | Média calculada corretamente após 3 avaliações | `MediaAvaliacoes = AVG das notas` |
| CT-10 | Avaliações exibidas no perfil público sem autenticação | `200 OK`, lista com nota, comentário, data |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Avaliação apenas do Cliente sobre o Prestador (unilateral) | RF-08 PDF | Incorreto. Arquitetura define avaliação **bilateral**: Cliente avalia Prestador (CL-11) **e** Prestador avalia Cliente (PR-11). |
| Avaliações fictícias ("seed") na fase inicial | RF-08 PDF | Não documentado na arquitetura. Prática problemática para conformidade e confiança dos usuários. |
| Edição de avaliação após criação | — | Avaliações são imutáveis (`CriadoEm` — sem `AtualizadoEm`). |
| Resposta do Prestador a uma avaliação | — | Não documentado na arquitetura. |
