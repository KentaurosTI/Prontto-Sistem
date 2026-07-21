# RF Review Report — Prontto

**Gerado em**: 2026-06-03
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Escopo**: RF-01 a RF-10 (PDF original v1.0, 30/05/2026)
**Documentos gerados**: `docs/RF-01` a `docs/RF-10`

---

## Sumário Geral

| RF | Nome | Ajustes | Status |
|----|------|---------|--------|
| RF-01 | Cadastro e Autenticação | 5 | Corrigido |
| RF-02 | Perfil Público do Prestador | 5 | Corrigido |
| RF-03 | Busca e Filtragem | 4 | Corrigido |
| RF-04 | Solicitação e Gestão de Serviços | 6 | Corrigido |
| RF-05 | Pagamento e Split | 6 | Corrigido |
| RF-06 | Chat Interno | 4 | Corrigido |
| RF-07 | Painel Administrativo | 4 | Corrigido |
| RF-08 | Avaliações | 4 | Corrigido |
| RF-09 | Gestão de Prazos de Entrega | 1 | Reclassificado |
| RF-10 | SEO, Hospedagem e Configurações | 4 | Corrigido |
| **Total** | | **43** | |

---

## RF-01 — Cadastro e Autenticação

**Arquivo**: `docs/RF-01-Cadastro-e-Autenticacao.md`

### Divergências encontradas

1. PDF menciona login social (Google, Facebook, Apple) — não documentado na arquitetura.
2. PDF usa o termo "Contratante" — arquitetura usa "Cliente" (`TipoConta = Cliente`).
3. PDF afirma que Prestador informa dados bancários no cadastro — arquitetura separa o cadastro de dados bancários em caso de uso próprio (PR-04), posterior ao registro.
4. PDF menciona "aprovação manual de cadastros pelo Admin" — não existe na arquitetura; cadastro é automático.
5. PDF menciona JWT sem detalhar duração — arquitetura define **Access Token de 15 minutos** (não 30 dias) + Refresh Token de 30 dias com rotação obrigatória, armazenado como hash SHA-256.

### Ajustes efetuados

1. Login social movido para Escopo Negativo.
2. Terminologia padronizada: "Contratante" → "Cliente".
3. Dados bancários removidos do cadastro; referência a PR-04 adicionada.
4. Aprovação manual movida para Escopo Negativo.
5. Fluxo completo de autenticação documentado: Access Token (15 min) + Refresh Token (30 dias, HttpOnly cookie, rotação obrigatória, hash SHA-256 no banco).

### Pendências / ADRs

- Nenhuma.

---

## RF-02 — Perfil Público do Prestador

**Arquivo**: `docs/RF-02-Perfil-Publico-Prestador.md`

### Divergências encontradas

1. PDF afirma que Prestador pode editar perfil "a qualquer momento" sem restrição — ADR-09 define Slug como imutável após publicação.
2. PDF não menciona moderação de imagens — arquitetura exige moderação Cloudinary; imagens não exibidas até `Aprovada = true`.
3. PDF descreve imagens como galeria simples — arquitetura tem entidade `ImagemPortfolio` com campos `Moderada`, `Aprovada`, `Ordem`, `DeletadoEm`.
4. PDF não menciona formato de URL pública — arquitetura define `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`.
5. PDF afirma que botão de contato "redireciona ao chat interno" diretamente — arquitetura: chat só existe após criação de serviço (RF-04); botão "Contratar" inicia fluxo de solicitação.

### Ajustes efetuados

1. Imutabilidade do Slug documentada (ADR-09); edição de nome exibido é permitida, slug não.
2. Moderação automática via Cloudinary documentada com fluxo completo (FP-03).
3. Tabela `imagens_portfolio` com todos os campos incluída.
4. Formato de URL `/{cidadeSlug}/{categoriaSlug}/{slug}` documentado.
5. Botão "Contratar" documentado corretamente: inicia solicitação de serviço (RF-04).

### Pendências / ADRs

- Nenhuma.

---

## RF-03 — Busca e Filtragem

**Arquivo**: `docs/RF-03-Busca-e-Filtragem.md`

### Divergências encontradas

1. PDF afirma que "usuário não autenticado é bloqueado e redirecionado ao cadastro" — **incorreto**. Arquitetura §10.2 e caso de uso CL-04 definem busca como pública.
2. PDF usa categoria como texto livre ("limpeza, jardinagem") — arquitetura define `Categoria` como entidade canônica com FK.
3. PDF usa região/cidade como texto livre — arquitetura define `Cidade` como entidade canônica com FK.
4. PDF menciona pesquisa textual por serviço — arquitetura menciona apenas como "futuramente"; não implementado na V1.

### Ajustes efetuados

1. Busca corrigida para pública: visitantes não autenticados podem buscar livremente. Autenticação exigida apenas para "Contratar".
2. Categoria documentada como entidade canônica (`Categoria`), com `categoriaSlug` como parâmetro de busca.
3. Cidade documentada como entidade canônica (`Cidade`), com `cidadeSlug` como parâmetro de busca.
4. Pesquisa textual movida para Escopo Negativo.

### Pendências / ADRs

- Nenhuma.

---

## RF-04 — Solicitação e Gestão de Serviços

**Arquivo**: `docs/RF-04-Solicitacao-e-Gestao-Servicos.md`

### Divergências encontradas

1. PDF descreve ciclo simplificado "solicitado → aceito → em andamento → concluído" (4 estados) — **incorreto**. Arquitetura tem 7+ estados: `EmNegociacao → AguardandoPagamento → Pago → EmAndamento → AguardandoConfirmacaoCliente → Concluido/EmDisputa/Cancelado`.
2. PDF afirma pagamento coletado "no momento da solicitação" — **incorreto**. Pagamento ocorre após acordo de preço, no estado `AguardandoPagamento`.
3. PDF não menciona fluxo de negociação (Propostas) — arquitetura define negociação via `MensagemServico` com `TipoMensagem = Proposta` (ADR-04).
4. PDF não menciona Disputa — arquitetura tem fluxo completo de Disputa com entidade `Disputa` e Admin como árbitro.
5. PDF não menciona auto-conclusão — arquitetura define auto-conclusão após 7 dias sem resposta do Cliente (Job SY-01).
6. PDF usa "BD-03" como referência — tabela é `servicos` no PostgreSQL.

### Ajustes efetuados

1. Máquina de estados completa documentada com tabela de transições válidas.
2. Pagamento documentado corretamente: gerado após acordo (`AguardandoPagamento`), não na criação.
3. Fluxo completo de negociação (Propostas, Contraproposta, Expiração) documentado.
4. Fluxo completo de Disputa documentado (FP-07 e FP-08) com entidade `Disputa`.
5. Auto-conclusão por inatividade documentada (FP-06, Job SY-01, 7 dias).
6. Referência "BD-03" substituída por tabela `servicos`.

### Pendências / ADRs

- Nenhuma.

---

## RF-05 — Pagamento e Split

**Arquivo**: `docs/RF-05-Pagamento-e-Split.md`

### Divergências encontradas

1. PDF define gateway como "AbacatePay ou Banda Pay" — **incorreto**. Arquitetura define **Pagar.me** via `IProcessadorPagamento` (ADR-01).
2. PDF afirma "split nativo do gateway" — **incorreto**. ADR-10: split é controlado pela aplicação, não pelo gateway.
3. PDF afirma pagamento na criação da solicitação — **incorreto**. Pagamento após acordo de preço.
4. PDF não menciona PIX como método de pagamento — arquitetura usa PIX com QR Code, `PixCopiaCola`, `PixExpiracaoEm`.
5. PDF não menciona webhook HMAC-SHA256 — arquitetura define validação de assinatura obrigatória.
6. PDF não menciona estados da cobrança — arquitetura tem `Pendente → Pago → Retido → Liberado/Reembolsado/Cancelado`.

### Ajustes efetuados

1. Gateway corrigido para Pagar.me + `IProcessadorPagamento`.
2. Split documentado como controlado pela aplicação (ADR-10).
3. Pagamento documentado após acordo de preço.
4. PIX com QR Code, Copia e Cola e expiração documentado.
5. Webhook com validação HMAC-SHA256 documentado (FP-02).
6. Máquina de estados de `Cobranca` completa documentada.

### Pendências / ADRs

- Nenhuma.

---

## RF-06 — Chat Interno

**Arquivo**: `docs/RF-06-Chat-Interno.md`

### Divergências encontradas

1. PDF afirma "chat em tempo real" — **incorreto para V1**. Arquitetura §11.4: V1 usa polling a cada 10 segundos. WebSocket (SignalR) planejado para V2.
2. PDF define bloqueio automático de números de telefone — não documentado na arquitetura. Sem suporte arquitetural na V1.
3. PDF define bloqueio automático de palavrões — não documentado na arquitetura.
4. PDF não menciona moderação de imagens no chat — arquitetura exige moderação Cloudinary para imagens do chat.

### Ajustes efetuados

1. Chat V1 documentado como polling 10s; SignalR documentado como plano V2.
2. Bloqueio de telefones movido para Escopo Negativo.
3. Bloqueio de palavrões movido para Escopo Negativo.
4. Fluxo completo de moderação de imagens no chat documentado (FP-02) com `ImagemModerada` e `ImagemAprovada`.

### Pendências / ADRs

- **ADR pendente**: Implementar filtro de conteúdo para telefones e palavrões. Exige decisão sobre estratégia (regex, serviço externo, dicionário) antes de implementar. Não deve ser implementado sem ADR aprovado.

---

## RF-07 — Painel Administrativo

**Arquivo**: `docs/RF-07-Painel-Administrativo.md`

### Divergências encontradas

1. PDF afirma acesso exclusivo por "login de administrador (admin@prontto.com)" — **incorreto**. Arquitetura usa `[Authorize(Roles = "admin")]` baseado em claim JWT `role = admin`. Não é restrito a e-mail específico.
2. PDF menciona "controle de aprovação automático ou manual" — não existe na arquitetura.
3. PDF não menciona `AuditLog` — arquitetura define trilha de auditoria imutável com 12+ ações obrigatórias.
4. PDF não menciona moderação de imagens como responsabilidade do Admin — arquitetura define AD-08 (moderação manual como fallback).

### Ajustes efetuados

1. Controle de acesso corrigido: `[Authorize(Roles = "admin")]`, não e-mail específico.
2. Aprovação manual de cadastros movida para Escopo Negativo.
3. Trilha de auditoria (`AuditLog`) documentada completamente (FP-09, AD-12).
4. Moderação de imagens como caso de uso Admin documentada (FP-05, AD-08).

### Pendências / ADRs

- Nenhuma.

---

## RF-08 — Avaliações

**Arquivo**: `docs/RF-08-Avaliacoes.md`

### Divergências encontradas

1. PDF descreve avaliação apenas do Cliente sobre o Prestador (unilateral) — **incorreto**. Arquitetura define avaliação **bilateral**: Cliente avalia Prestador (CL-11) E Prestador avalia Cliente (PR-11).
2. PDF menciona "avaliações fictícias na fase inicial" — não documentado na arquitetura. Exclui confiabilidade e integridade dos dados.
3. PDF não menciona janela de avaliação — arquitetura define **30 dias** após conclusão.
4. PDF não menciona constraint de unicidade — arquitetura define `UNIQUE(service_id, reviewer_id)`.

### Ajustes efetuados

1. Avaliação documentada como bilateral com dois fluxos (FP-01 Cliente→Prestador, FP-02 Prestador→Cliente).
2. Avaliações fictícias movidas para Escopo Negativo.
3. Janela de 30 dias documentada (RN-03, FE-01, CA-03).
4. Constraint `UNIQUE(service_id, reviewer_id)` documentado (RN-04, FA-02).

### Pendências / ADRs

- Nenhuma.

---

## RF-09 — Gestão de Prazos e Entrega do Sistema

**Arquivo**: `docs/RF-09-Gestao-Prazos-Entrega.md`

### Divergências encontradas

1. RF-09 descreve **processo contratual de entrega de projeto** (prazos, UAT, suporte pós-entrega), não funcionalidade do sistema Prontto. O `ARCHITECTURE.md` não documenta nenhum módulo de gestão de prazos como feature da plataforma.

### Ajustes efetuados

1. RF reclassificado explicitamente como requisito contratual/não-funcional. Documento preservado para fins de registro, com nota arquitetural clara indicando que **nada deve ser implementado como funcionalidade do software**.

### Pendências / ADRs

- Nenhuma de caráter técnico. A equipe deve garantir que o documento seja tratado apenas como referência contratual.

---

## RF-10 — SEO, Hospedagem e Configurações Técnicas

**Arquivo**: `docs/RF-10-SEO-Hospedagem-Configuracoes.md`

### Divergências encontradas

1. PDF menciona "deploy no Hostinger" — arquitetura define Docker Compose + PostgreSQL 17 + NGINX. Hostinger não é mencionado na arquitetura.
2. PDF menciona "AbacatePay ou Banda Pay" — arquitetura define Pagar.me.
3. PDF não menciona rate limiting — arquitetura define limites específicos com `Microsoft.AspNetCore.RateLimiting`.
4. PDF não menciona CORS, sanitização com `DomSanitizer`, `FluentValidation`, criptografia de CPF (AES-256) ou variáveis de ambiente específicas.

### Ajustes efetuados

1. Infraestrutura corrigida para Docker Compose + NGINX + PostgreSQL 17. Hostinger movido para Escopo Negativo como detalhe operacional.
2. Gateway corrigido para Pagar.me.
3. Rate limiting documentado com limites por endpoint.
4. CORS, `DomSanitizer`, `FluentValidation`, criptografia de CPF e variáveis de ambiente documentados.

### Pendências / ADRs

- Nenhuma.

---

## Funcionalidades do PDF NÃO presentes na arquitetura

> Inventário completo de itens do PDF original que **não existem** no `ARCHITECTURE.md`. Não devem ser implementados sem nova decisão arquitetural (ADR).

| Item | RF | Impacto estimado |
|------|----|-----------------|
| Login social (Google, Facebook, Apple) | RF-01 | Alto — integração OAuth2, novos fluxos de cadastro, mapeamento de conta social → `Usuario` |
| Aprovação manual de cadastros pelo Admin | RF-01 | Médio — novo estado de usuário (`Pendente`/`Ativo`), fila de aprovação, notificações |
| Bloqueio automático de números de telefone no chat | RF-06 | Baixo/Médio — regex ou serviço externo; novo middleware de chat |
| Bloqueio automático de palavrões no chat | RF-06 | Baixo/Médio — dicionário ou API de moderação de texto |
| Controle de aprovação de cadastros (automático/manual) | RF-07 | Médio — ver item de RF-01 |
| Avaliações fictícias ("seed") na fase inicial | RF-08 | Baixo técnico; Alto risco de integridade — não recomendado sem ADR de dados de seed |
| Notificações via e-mail | RF-04, RF-07 | Médio — integração com SendGrid ou SMTP; novos handlers de evento |
| Notificações via WhatsApp | RF-07, RF-10 | Alto — integração com Meta Business API ou Twilio |
| Integração com CRM | RF-10 | Alto — depende do CRM escolhido; não especificado |
| Deploy no Hostinger especificamente | RF-10 | Baixo técnico — é decisão operacional de infraestrutura |
| Pesquisa textual por serviço (fulltext search) | RF-03 | Médio — PostgreSQL fulltext ou Elasticsearch; nova query de busca |
| Chat disponível antes da criação de serviço | RF-06 | Médio — exige redesign do modelo de chat |
| Filtro por bairro (granularidade abaixo de cidade) | RF-03 | Médio — nova entidade `Bairro`, relacionamentos adicionais |

---

## Funcionalidades da arquitetura NÃO cobertas pelos RFs originais (gaps)

> Itens documentados no `ARCHITECTURE.md` que **não estão cobertos** pelos RF-01 a RF-10 do PDF original. São funcionalidades planejadas que exigem documentação própria.

| Funcionalidade | Entidade/Caso de Uso | Observação |
|---------------|---------------------|------------|
| Renovação de sessão (Refresh Token, rotação) | `RefreshToken`, CL-15, PR-15 | Não mencionado no PDF. Crítico para segurança. |
| Logout com revogação de Refresh Token | CL-16, PR-16 | Não mencionado. |
| Reclassificação de disputa (EmAnalise → Resolvida) | `Disputa`, `StatusDisputa` | PDF menciona disputa vagamente; arquitetura tem fluxo detalhado. |
| Trilha de auditoria (`AuditLog`) | `AuditLog`, AD-12 | Não mencionada no PDF. |
| Sistema de notificações in-app (`Notificacao`) | `Notificacao`, SY-09 | Não coberto pelo PDF. |
| Portfólio com moderação automática | `ImagemPortfolio`, AD-08 | PDF menciona portfólio mas sem moderação. |
| Slugs com sufixo aleatório e imutabilidade | ADR-08, ADR-09 | Não discutido no PDF. |
| Upload direto browser → Cloudinary | ADR-03 | Não mencionado no PDF. |
| Expiração automática de PIX | `PixExpiracaoEm`, SY-02 | Não coberto. |
| Extrato de repasses do Prestador | PR-12, `Cobranca.Liberado` | Não explicitado no PDF. |
| Dados bancários separados do cadastro | `DadosBancarios`, PR-04 | PDF mistura com cadastro. |
| Soft delete com filtro global EF Core | `DeletadoEm` em `Usuario`, `Servico`, `ImagemPortfolio` | Não mencionado. |
| Criptografia de CPF (AES-256) | `Usuario.Cpf` | Não mencionado. |
| Direito de exclusão de conta (LGPD) | `DELETE /api/minha-conta` | Não mencionado no PDF. |
| Jobs automatizados (conclusão, PIX, liberação, moderação, médias) | SY-01 a SY-09 | Não cobertos diretamente pelos RFs do PDF. |
| Consentimento explícito no cadastro | §10.6 | Não mencionado no PDF. |
| Categorias e cidades como entidades canônicas | `Categoria`, `Cidade` | PDF usa strings livres — arquitetura usa FKs. |
| Paginação cursor-based no chat | `afterId` | Não mencionado. |
| Cache de categorias (1h) e perfis (5min) | `IMemoryCache` | Não mencionado. |

---

## Decisões Arquiteturais Pendentes (ADRs Sugeridos)

| ADR Sugerido | Contexto | Prioridade |
|-------------|---------|-----------|
| Filtro de conteúdo no chat (telefones e palavrões) | RF-06 — feature pedida no PDF mas sem suporte arquitetural | Média |
| Login social OAuth2 | RF-01 — pedido no PDF; exige mapeamento de conta social → `Usuario` | Baixa (V2) |
| Estratégia de seed de dados (avaliações de demonstração) | RF-08 — pedido no PDF; risco de integridade | Baixa — não recomendada |
| Notificações por e-mail (transacional) | RF-04, RF-07 — mencionado como importante pelo cliente | Média (V2) |
| Integração WhatsApp para notificações | RF-07, RF-10 — mencionado como "futuramente" | Baixa (V2+) |

---

*Relatório gerado com base no `ARCHITECTURE.md` v1.1 (2026-06-03). Qualquer alteração na arquitetura pode impactar as decisões documentadas acima.*
