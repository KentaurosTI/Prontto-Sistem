# RF-05 — Pagamento e Split Financeiro

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Gerenciar o ciclo financeiro de cada serviço: geração de cobrança PIX via Pagar.me, retenção do pagamento até conclusão do serviço, e liberação com split 80/20 ao Prestador — ou reembolso em caso de cancelamento/disputa favorável ao Cliente.

---

## Descrição

O sistema financeiro é controlado pela aplicação (ADR-10), não por recurso de escrow nativo do gateway. O Prestador de pagamentos é a **Pagar.me**, acessada exclusivamente via interface `IProcessadorPagamento` (ADR-01).

O ciclo financeiro segue os estados: `Pendente → Pago → Retido → Liberado` (caminho feliz) ou `Reembolsado`/`Cancelado` (cancelamentos e disputas). O split 80/20 é executado pela aplicação no momento da liberação — não pelo gateway automaticamente.

O pagamento é efetuado via **PIX** com QR Code e linha Copia e Cola, com prazo de expiração. O webhook da Pagar.me confirma o pagamento.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente | Realiza o pagamento PIX |
| Prestador | Recebe o repasse após conclusão do serviço |
| Sistema (Pagar.me) | Gera a cobrança PIX, recebe o pagamento, notifica via webhook |
| Sistema (Backend) | Processa webhook, controla retenção, executa split e transferência |
| Admin | Visualiza cobranças, pode cancelar e reembolsar |

---

## Pré-condições

- Serviço em `AguardandoPagamento` (acordo de preço firmado, RF-04 FP-03).
- Prestador com `DadosBancarios` cadastrados (PR-04) para receber o repasse.
- Variável de ambiente `PAGARME_WEBHOOK_SECRET` configurada para validação HMAC.

---

## Fluxos Principais

### FP-01 — Geração de Cobrança e PIX

1. Ao serviço avançar para `AguardandoPagamento` (evento `AcordoFirmado`), `ServicoFinanceiro.CriarCobrancaAsync()` é chamado automaticamente.
2. Sistema cria `Cobranca` com:
   - `ValorTotal = Servico.Preco`
   - `TaxaAdmin = ValorTotal * Servico.TaxaAdminRate` (padrão 20%)
   - `ValorPrestador = ValorTotal - TaxaAdmin`
   - `Status = Pendente`
3. Sistema chama `IProcessadorPagamento.GerarPixAsync()` na Pagar.me:
   - Pagar.me retorna `PagarmeOrderId`, `PixQrCode` (base64/SVG), `PixCopiaCola`, `PixExpiracaoEm`.
4. Sistema persiste na `Cobranca`: `PagarmeOrderId`, `PixQrCode`, `PixCopiaCola`, `PixExpiracaoEm`.
5. Sistema publica eventos `CobrancaCriada` e `PixGerado`.
6. Frontend exibe QR Code e linha Copia e Cola para o Cliente.

### FP-02 — Recebimento do Webhook de Confirmação

1. Cliente paga o PIX em seu banco.
2. Pagar.me envia `POST /webhooks/pagarme` com payload do evento `payment.confirmed`.
3. Backend valida assinatura **HMAC-SHA256** usando `PAGARME_WEBHOOK_SECRET`. Falha na validação → `401` (descarta silenciosamente).
4. Backend verifica idempotência: busca `Cobranca` por `PagarmeOrderId`. Se já processado (`Status != Pendente`) → responde `200` e encerra (sem reprocessar).
5. Backend atualiza `Cobranca`:
   - `Status: Pendente → Pago → Retido` (duas transições imediatas)
   - `PagadoEm = UtcNow`
   - `RetidoEm = UtcNow`
   - `PagarmePagamentoId` preenchido
6. Backend atualiza `Servico.Status = Pago → EmAndamento` (transição imediata após pagamento).
7. Sistema publica evento `PagamentoConfirmado`.
8. Backend responde `200 OK` imediatamente (processamento assíncrono para evitar timeout).
9. Notificações enviadas ao Cliente e Prestador.

### FP-03 — Liberação de Pagamento ao Prestador

Gatilho: evento `ServicoConcluido` (RF-04 FP-05, FP-06 e FP-08 — decisão admin em favor do Prestador).

1. Handler `JobLiberacaoPagamento` recebe o evento `ServicoConcluido`.
2. Sistema busca `Cobranca` do serviço com `Status = Retido`.
3. Sistema chama `IProcessadorPagamento.TransferirAsync()`:
   - Valor: `Cobranca.ValorPrestador` (80% do total)
   - Destino: dados PIX do Prestador (`DadosBancarios.ChavePix`)
4. Ao confirmar a transferência:
   - `Cobranca.Status = Liberado`
   - `Cobranca.LiberadoEm = UtcNow`
5. Sistema publica evento `PagamentoLiberado`.
6. `AuditLog` obrigatório: `Acao = "pagamento.liberado"`.
7. Notificação ao Prestador.

**Retry**: máximo 3 tentativas com backoff exponencial em caso de falha na transferência.

### FP-04 — Reembolso ao Cliente

Gatilho: Admin cancela serviço após pagamento (Cobrança `Retido`) ou Admin decide disputa em favor do Cliente.

1. Admin aciona reembolso ou resolve disputa a favor do Cliente.
2. Sistema chama `IProcessadorPagamento.ReembolsarAsync()`:
   - Valor: `Cobranca.ValorTotal` (100% devolvido ao Cliente)
3. `Cobranca.Status = Reembolsado`.
4. Sistema publica evento `ReembolsoIniciado`.
5. `AuditLog` obrigatório: `Acao = "pagamento.reembolsado"`.
6. Notificação ao Cliente.

### FP-05 — Expiração de PIX (Job SY-02)

1. Job `JobExpiracaoPix` executa a cada 15 minutos.
2. Busca cobranças com `Status = Pendente` e `PixExpiracaoEm < UtcNow`.
3. Para cada cobrança:
   - `Cobranca.Status = Cancelado`
   - `Servico.Status = Cancelado` (se ainda em `AguardandoPagamento`)
4. Sistema publica evento `PixExpirado`.
5. Notificação ao Cliente sugerindo nova tentativa.

---

## Fluxos Alternativos

### FA-01 — Cliente não paga o PIX antes da expiração

- **Ponto de desvio**: `PixExpiracaoEm` atingido.
- **Ação**: FP-05 — cobrança e serviço cancelados.
- **Continuação**: Cliente pode reabrir a negociação (novo serviço).

### FA-02 — Falha na transferência ao Prestador

- **Ponto de desvio**: FP-03, passo 3 — `IProcessadorPagamento.TransferirAsync()` retorna erro.
- **Ação**: Retry automático (máximo 3 tentativas, backoff exponencial). Após esgotar retries: alerta de monitoramento; Admin visualiza na fila de repasses pendentes.
- **Continuação**: Admin pode acionar reprocessamento manual.

---

## Fluxos de Exceção

### FE-01 — Webhook com assinatura inválida

- **Ponto de desvio**: FP-02, passo 3 — HMAC inválido.
- **Ação**: `401 Unauthorized`. Nenhuma alteração de estado.

### FE-02 — Webhook duplicado (idempotência)

- **Ponto de desvio**: FP-02, passo 4 — `Cobranca.Status != Pendente`.
- **Ação**: `200 OK` sem reprocessar. Idempotência garantida pelo `UNIQUE(pagarme_order_id)`.

---

## Pós-condições

- Após pagamento: `Cobranca.Status = Retido`, `Servico.Status = EmAndamento`.
- Após conclusão: `Cobranca.Status = Liberado`, `LiberadoEm` preenchido.
- Em cancelamento/disputa (favor Cliente): `Cobranca.Status = Reembolsado`.
- `AuditLog` gravado para confirmação, liberação e reembolso.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | O gateway de pagamento é a **Pagar.me**, acessada exclusivamente via `IProcessadorPagamento`. Nenhum código de domínio ou aplicação importa SDK da Pagar.me diretamente (ADR-01). |
| RN-02 | O split 80/20 é **controlado pela aplicação**, não pelo gateway (ADR-10). A Pagar.me não executa o split automaticamente. |
| RN-03 | O pagamento é realizado via **PIX** com QR Code e linha Copia e Cola. |
| RN-04 | `TaxaAdmin = ValorTotal * TaxaAdminRate`. `ValorPrestador = ValorTotal - TaxaAdmin`. Valor padrão de `TaxaAdminRate = 0.2000` (20%). Todos os valores são `decimal` — nunca `double` ou `float`. |
| RN-05 | O pagamento fica **retido** na plataforma após confirmação do webhook (`Cobranca.Status = Retido`). Não é repassado automaticamente ao Prestador. |
| RN-06 | O repasse ao Prestador só ocorre após `Servico.Status = Concluido` (por confirmação do Cliente, auto-conclusão ou decisão do Admin em favor do Prestador). |
| RN-07 | Webhook da Pagar.me é validado por **HMAC-SHA256**. Requests sem assinatura válida são rejeitados. |
| RN-08 | Idempotência: `Cobranca.PagarmeOrderId` tem constraint `UNIQUE`. Webhooks duplicados são ignorados. |
| RN-09 | Em disputa com decisão a favor do Cliente: **100%** do valor é reembolsado. |
| RN-10 | Em disputa com decisão a favor do Prestador: split **80/20** é executado normalmente. |
| RN-11 | **Impostos incidem sobre 20%** (comissão da plataforma), não sobre o valor total. Responsabilidade contábil da plataforma. (Regra fiscal — sem impacto na implementação técnica da V1.) |
| RN-12 | `LiberadoEm` permanece `null` até confirmação real da transferência. Não é preenchido antes do sucesso da chamada à Pagar.me. |

---

## Máquina de Estados — `StatusCobranca`

| De | Para | Gatilho |
|----|------|---------|
| `Pendente` | `Pago` | Webhook Pagar.me (`payment.confirmed`) |
| `Pendente` | `Cancelado` | PIX expirado (Job SY-02) / Admin |
| `Pago` | `Retido` | Imediato (mesmo webhook) |
| `Retido` | `Liberado` | Serviço `Concluido` (handler de evento) |
| `Retido` | `Reembolsado` | Admin (disputa / cancelamento / fraude) |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `CobrancaCriada` | Cobrança criada após acordo | `{ CobrancaId, ServicoId, ValorTotal, PixExpiracaoEm }` |
| `PixGerado` | PIX gerado pela Pagar.me | `{ CobrancaId, QrCode, CopiaCola, ExpiracaoEm }` |
| `PagamentoConfirmado` | Webhook confirma pagamento | `{ CobrancaId, ServicoId, ValorPago, PagadoEm }` |
| `PagamentoLiberado` | Repasse ao Prestador executado | `{ CobrancaId, ServicoId, PrestadorId, ValorPrestador }` |
| `ReembolsoIniciado` | Reembolso ao Cliente executado | `{ CobrancaId, ServicoId, ClienteId, ValorTotal }` |
| `PixExpirado` | PIX não pago dentro do prazo | `{ CobrancaId, ServicoId }` |

---

## Entidades Envolvidas

### `Cobranca`

**Tabela**: `cobrancas`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `service_id` | FK único → `servicos.id` |
| `ValorTotal` | `decimal` | `total_amount` | Valor acordado |
| `TaxaAdmin` | `decimal` | `admin_fee` | `ValorTotal * TaxaAdminRate` |
| `ValorPrestador` | `decimal` | `provider_amount` | `ValorTotal - TaxaAdmin` |
| `Status` | `StatusCobranca` | `status` | Máquina de estados |
| `PagarmeOrderId` | `string?` | `pagarme_order_id` | UNIQUE — idempotência webhook |
| `PagarmePagamentoId` | `string?` | `pagarme_payment_id` | ID do pagamento confirmado |
| `PixQrCode` | `string?` | `pix_qr_code` | QR Code base64/SVG |
| `PixCopiaCola` | `string?` | `pix_copia_cola` | Linha digitável PIX |
| `PixExpiracaoEm` | `DateTime?` | `pix_expires_at` | UTC |
| `PagadoEm` | `DateTime?` | `paid_at` | UTC |
| `RetidoEm` | `DateTime?` | `held_at` | UTC |
| `LiberadoEm` | `DateTime?` | `released_at` | UTC — preenchido após confirmação da transferência |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `AtualizadoEm` | `DateTime` | `updated_at` | UTC |

### `DadosBancarios`

**Tabela**: `dados_bancarios`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `user_id` | FK único → `usuarios.id` |
| `TipoChavePix` | `TipoChavePix` | `pix_key_type` | `cpf`/`cnpj`/`email`/`telefone`/`aleatoria` |
| `ChavePix` | `string` | `pix_key` | Valor da chave |
| `NomeCompleto` | `string` | `full_name` | Nome completo |
| `CpfCnpj` | `string` | `cpf_cnpj` | Documento do titular |
| `NomeBanco` | `string?` | `bank_name` | |
| `Agencia` | `string?` | `agency` | |
| `NumeroConta` | `string?` | `account_number` | |
| `TipoConta` | `string?` | `account_type` | Corrente/Poupança |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `AtualizadoEm` | `DateTime` | `updated_at` | UTC |

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/servicos/{id}/cobranca` | Bearer (participante) | Ver cobrança e PIX |
| `POST` | `/webhooks/pagarme` | Sem JWT (HMAC) | Webhook de confirmação |
| `GET` | `/api/admin/charges` | Bearer (Admin) | Listar cobranças |
| `GET` | `/api/admin/financeiro` | Bearer (Admin) | Extrato financeiro da plataforma |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Cobrança criada automaticamente ao acordo de preço, com `Status = Pendente` e PIX gerado. |
| CA-02 | Webhook da Pagar.me com HMAC inválido retorna `401`. |
| CA-03 | Webhook duplicado retorna `200` sem reprocessar. |
| CA-04 | Após confirmação do webhook: `Cobranca.Status = Retido`, `Servico.Status = EmAndamento`. |
| CA-05 | Após conclusão do serviço: split 80/20 executado pela aplicação, `Cobranca.Status = Liberado`. |
| CA-06 | Reembolso devolve 100% ao Cliente. |
| CA-07 | PIX expirado sem pagamento cancela cobrança e serviço. |
| CA-08 | `LiberadoEm` só é preenchido após confirmação real da transferência. |
| CA-09 | Todos os valores monetários usam `decimal` — nunca `double` ou `float`. |
| CA-10 | Dados bancários do Prestador acessíveis apenas pelo próprio e Admin. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Acordo firmado → cobrança criada | `Cobranca.Status = Pendente`, PIX retornado |
| CT-02 | Webhook válido `payment.confirmed` | `Cobranca.Status = Retido`, `Servico.Status = EmAndamento` |
| CT-03 | Webhook com HMAC inválido | `401 Unauthorized`, sem alteração de estado |
| CT-04 | Mesmo webhook enviado 2x | Segundo processamento retorna `200` sem duplicar transição |
| CT-05 | Serviço concluído pelo Cliente | Split executado, `Cobranca.Status = Liberado` |
| CT-06 | Disputa resolvida em favor do Prestador | Split executado normalmente |
| CT-07 | Disputa resolvida em favor do Cliente | 100% reembolsado |
| CT-08 | PIX expira sem pagamento | `Cobranca.Status = Cancelado`, `Servico.Status = Cancelado` |
| CT-09 | Falha no repasse ao Prestador | Retry automático; após 3 falhas, alerta gerado |
| CT-10 | `TaxaAdmin` calculada com `decimal` | Valor exato sem arredondamento de ponto flutuante |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Gateway AbacatePay ou Banda Pay | RF-05 PDF | Incorreto. Arquitetura define **Pagar.me** como gateway via `IProcessadorPagamento` (ADR-01). |
| Split automático nativo do gateway | RF-05 PDF | Incorreto. ADR-10: split é **controlado pela aplicação**, não pelo gateway. |
| Pagamento coletado na criação da solicitação | RF-05 PDF | Incorreto. Pagamento ocorre após acordo de preço (`AguardandoPagamento`). |
| Extrato de pagamentos para o Prestador em interface separada | RF-05 PDF | PR-12 cobre extrato de repasses via `GET /api/auth/repasses`. Não é funcionalidade do RF-05. |
