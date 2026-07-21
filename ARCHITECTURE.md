# Prontto — Arquitetura Mestre

> Versão 1.2 — Documento normativo. Toda implementação futura deve estar alinhada com as decisões aqui registradas.
> Última alteração: 2026-06-12 — Migração de banco de dados de PostgreSQL 17 para MySQL 8.0 (Hostinger). ADR-07 atualizado. Provider EF Core alterado para Pomelo.

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Domínio e Linguagem Ubíqua](#2-domínio-e-linguagem-ubíqua)
3. [Bounded Contexts](#3-bounded-contexts)
4. [Entidades do Domínio](#4-entidades-do-domínio)
5. [Casos de Uso por Ator](#5-casos-de-uso-por-ator)
6. [Fluxos Principais](#6-fluxos-principais)
7. [Máquina de Estados](#7-máquina-de-estados)
8. [Eventos de Domínio](#8-eventos-de-domínio)
9. [Jobs Automatizados](#9-jobs-automatizados)
10. [Segurança](#10-segurança)
11. [Escalabilidade](#11-escalabilidade)
12. [Decisões Arquiteturais (ADRs)](#12-decisões-arquiteturais-adrs)
13. [Riscos Técnicos](#13-riscos-técnicos)
14. [Próximos Passos](#14-próximos-passos)

---

## 1. Visão Geral

### Propósito

Prontto é um marketplace brasileiro de serviços domésticos que conecta Clientes (quem contrata) e Prestadores (quem executa). A plataforma gerencia o ciclo completo: descoberta, negociação, pagamento, execução, confirmação e avaliação.

### Stack

| Camada | Tecnologia |
|--------|-----------|
| Backend | ASP.NET Core (C#), Clean Architecture |
| ORM | Entity Framework Core 9 |
| Banco de Dados | MySQL 8.0 (Hostinger: srv2103.hstgr.io) |
| Frontend | Angular 21, Standalone Components, Signals |
| Autenticação | JWT HS256 — Access Token 15 min + Refresh Token 30 dias (rotação obrigatória, revogável) |
| Pagamentos | Pagar.me (via abstração `IProcessadorPagamento`) |
| Uploads | Cloudinary (via abstração `IArmazenamentoArquivo`) |
| Contrato de API | OpenAPI 3.1 (`api-spec/openapi.yaml` — fonte da verdade) |
| Infraestrutura local | Docker Compose (MySQL 8.0, backend, frontend) |

### Princípios Arquiteturais

1. **Domain-first**: toda regra de negócio vive no domínio. Infraestrutura apenas implementa interfaces.
2. **Abstração de integrações externas**: Pagar.me e Cloudinary são acessados exclusivamente via interfaces. Nunca acoplar regras de negócio às SDKs diretamente.
3. **Contrato OpenAPI como fonte da verdade**: o backend implementa, o frontend consome. Qualquer mudança de API começa no `openapi.yaml`.
4. **Nomenclatura bilíngue**: projetos e pastas em inglês; classes, interfaces, variáveis e propriedades em português.
5. **Responsividade obrigatória**: todo componente Angular é mobile-first.
6. **Testes obrigatórios**: toda feature de aplicação tem cobertura unitária correspondente.

---

## 2. Domínio e Linguagem Ubíqua

| Termo | Significado |
|-------|------------|
| **Cliente** | Usuário com `TipoConta = Cliente` que contrata serviços |
| **Prestador** | Usuário com `TipoConta = Prestador` que executa serviços |
| **Solicitação** | Registro inicial criado pelo Cliente antes de um Prestador ser vinculado |
| **Serviço** | Ordem de serviço em qualquer fase do ciclo de vida (`Servico`) |
| **Proposta** | Oferta de preço enviada durante a negociação (via `MensagemServico` com tipo `Proposta`) |
| **Contraproposta** | Resposta a uma proposta com valor diferente |
| **Acordo** | Estado em que ambas as partes aceitaram o mesmo valor |
| **Cobrança** | Transação financeira associada a um Serviço (`Cobranca`) |
| **PIX** | Método de pagamento gerado pela Pagar.me |
| **Retenção** | Período em que o pagamento está sob custódia da plataforma após confirmação do PIX |
| **Liberação** | Transferência do valor ao Prestador após confirmação do serviço |
| **Taxa Admin** | Comissão da Prontto: 20% do valor acordado |
| **Slug** | Identificador legível na URL: `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}` |
| **Avaliação** | Nota de 1 a 5 com comentário opcional, bilateral entre Cliente e Prestador |
| **Portfólio** | Galeria de imagens do trabalho do Prestador |
| **Moderação** | Análise automática de imagens via Cloudinary antes de exibição |
| **Disputa** | Contestação aberta pelo Cliente após o Prestador marcar serviço como concluído; pagamento permanece retido até resolução pelo Admin |
| **RefreshToken** | Token de longa duração armazenado no banco, usado para renovar o Access Token sem novo login; invalidado a cada uso (rotação) |
| **AuditLog** | Registro imutável de uma ação realizada na plataforma — quem fez, o quê, em qual entidade, quando e de onde |
| **Notificação** | Mensagem de sistema enviada a um usuário sobre evento relevante (nova proposta, pagamento recebido, disputa, etc.) |

---

## 3. Bounded Contexts

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PRONTTO PLATFORM                              │
│                                                                       │
│  ┌──────────────────┐    ┌──────────────────┐    ┌────────────────┐ │
│  │   IDENTIDADE     │    │    MARKETPLACE   │    │  FINANCEIRO    │ │
│  │                  │    │                  │    │                │ │
│  │ Usuario          │───▶│ Servico          │───▶│ Cobranca       │ │
│  │ DadosBancarios   │    │ MensagemServico  │    │ Transferencia  │ │
│  │ PerfilPrestador  │    │ Proposta         │    │ Extrato        │ │
│  │ Avaliacao        │    │ Negociacao       │    │                │ │
│  └──────────────────┘    └──────────────────┘    └────────────────┘ │
│                                   │                                   │
│  ┌──────────────────┐    ┌──────────────────┐                        │
│  │   DESCOBERTA     │    │    MODERACAO     │                        │
│  │                  │    │                  │                        │
│  │ PaginaPrestador  │    │ ImagemPendente   │                        │
│  │ Busca            │    │ ResultadoAnalise │                        │
│  │ Categoria        │    │                  │                        │
│  └──────────────────┘    └──────────────────┘                        │
└─────────────────────────────────────────────────────────────────────┘
```

### Mapeamento Bounded Context → Camada de Aplicação

| Contexto | Namespace Application | Responsabilidade |
|---------|----------------------|-----------------|
| Identidade | `Prontto.Application.Auth` | Autenticação, perfil, dados bancários |
| Marketplace | `Prontto.Application.Servicos` | CRUD de serviços, chat, negociação |
| Financeiro | `Prontto.Application.Financeiro` | Cobranças, pagamentos, repasses |
| Descoberta | `Prontto.Application.Descoberta` | Busca pública, perfis de prestadores |
| Moderação | `Prontto.Application.Moderacao` | Análise de imagens do chat e portfólio |
| Admin | `Prontto.Application.Admin` | Painel administrativo, estatísticas |

---

## 4. Entidades do Domínio

### 4.1 `Usuario`

**Responsabilidade**: Representa qualquer pessoa cadastrada na plataforma. Pode ser Cliente ou Prestador. Um usuário Admin tem `Papel = Admin` independentemente de `TipoConta`.

**Tabela**: `usuarios`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK, UUID v4 |
| `Nome` | `string` | `nome` | Obrigatório |
| `Email` | `string` | `email` | Único, lowercase |
| `Telefone` | `string?` | `telefone` | Opcional |
| `HashSenha` | `string` | `hash_senha` | BCrypt |
| `TipoConta` | `TipoConta` | `tipo_conta` | `cliente` / `prestador` |
| `Papel` | `Papel` | `papel` | `usuario` / `admin` |
| `Especialidade` | `string?` | `especialidade` | Texto livre (legacy — manter para compatibilidade) |
| `CidadeId` | `Guid?` | `cidade_id` | FK → `cities.id` (substitui `city` texto livre) |
| `Cpf` | `string?` | `cpf` | 11 dígitos, único quando preenchido; armazenado criptografado (AES-256) |
| `FotoPerfilUrl` | `string?` | `url_foto_perfil` | URL Cloudinary |
| `Slug` | `string?` | `slug` | Único global, gerado ao completar perfil; **imutável após publicação** |
| `Descricao` | `string?` | `descricao` | Bio pública do prestador |
| `MediaAvaliacoes` | `decimal` | `media_avaliacoes` | Calculado via job após cada avaliação |
| `TotalAvaliacoes` | `int` | `total_avaliacoes` | Contagem |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `AtualizadoEm` | `DateTime` | `atualizado_em` | UTC |
| `DeletadoEm` | `DateTime?` | `deletado_em` | UTC — soft delete; filtro global EF Core exclui registros com valor preenchido |

**Relacionamentos**:
- 1:N com `Servico` (como cliente)
- 1:N com `Servico` (como prestador)
- 1:1 com `DadosBancarios`
- 1:N com `MensagemServico`
- 1:N com `CategoriaUsuario` (categorias múltiplas)
- 1:N com `CidadeUsuario` (cidades múltiplas)
- 1:N com `ImagemPortfolio`
- 1:N com `Avaliacao` (como avaliado)
- 1:N com `Avaliacao` (como avaliador)

---

### 4.2 `Servico`

**Responsabilidade**: Representa a ordem de serviço do início ao fim. É o agregado central da plataforma.

**Tabela**: `servicos`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Titulo` | `string` | `titulo` | Obrigatório |
| `Descricao` | `string?` | `descricao` | Opcional |
| `CategoriaId` | `Guid` | `categoria_id` | FK → `categories.id` (nunca string livre) |
| `CidadeId` | `Guid?` | `cidade_id` | FK → `cities.id` (cidade do serviço) |
| `ClienteId` | `Guid?` | `cliente_id` | FK → `users.id` |
| `PrestadorId` | `Guid?` | `prestador_id` | FK → `users.id` |
| `Preco` | `decimal` | `preco` | Valor acordado. `decimal`, nunca `double` |
| `TaxaAdminRate` | `decimal` | `taxa_admin_percentual` | Padrão `0.2000` |
| `Status` | `StatusServico` | `status` | Ver máquina de estados |
| `Endereco` | `string?` | `endereco` | Endereço completo de execução |
| `AgendadoEm` | `DateTime?` | `agendado_em` | UTC |
| `ConcluidoEm` | `DateTime?` | `concluido_em` | UTC, preenchido na conclusão |
| `AguardandoConfirmacaoDesde` | `DateTime?` | `aguardando_confirmacao_desde` | UTC, base para auto-conclusão em 7 dias |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `AtualizadoEm` | `DateTime` | `atualizado_em` | UTC |
| `DeletadoEm` | `DateTime?` | `deletado_em` | UTC — soft delete; filtro global EF Core |

**Relacionamentos**:
- N:1 com `Usuario` (cliente)
- N:1 com `Usuario` (prestador)
- 1:1 com `Cobranca`
- 1:N com `MensagemServico`
- 1:1 com `Avaliacao` do cliente
- 1:1 com `Avaliacao` do prestador

---

### 4.3 `Cobranca`

**Responsabilidade**: Representa o ciclo financeiro de um serviço. Criada automaticamente quando o serviço avança para `AguardandoPagamento`. Contém referências ao objeto de pagamento externo (Pagar.me).

**Tabela**: `cobrancas`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `servico_id` | FK único → `services.id` |
| `ValorTotal` | `decimal` | `valor_total` | Valor acordado |
| `TaxaAdmin` | `decimal` | `taxa_admin` | `ValorTotal * TaxaAdminRate` |
| `ValorPrestador` | `decimal` | `valor_prestador` | `ValorTotal - TaxaAdmin` |
| `Status` | `StatusCobranca` | `status` | Ver máquina de estados |
| `PagarmeOrderId` | `string?` | `pagarme_order_id` | ID do pedido na Pagar.me |
| `PagarmePagamentoId` | `string?` | `pagarme_payment_id` | ID do pagamento na Pagar.me |
| `PixQrCode` | `string?` | `pix_qr_code` | QR code base64 ou SVG |
| `PixCopiaCola` | `string?` | `pix_copia_cola` | Linha digitável PIX |
| `PixExpiracaoEm` | `DateTime?` | `pix_expira_em` | UTC |
| `PagadoEm` | `DateTime?` | `pago_em` | UTC |
| `RetidoEm` | `DateTime?` | `retido_em` | UTC, quando PIX confirmado |
| `LiberadoEm` | `DateTime?` | `liberado_em` | UTC, quando repasse ao prestador |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `AtualizadoEm` | `DateTime` | `atualizado_em` | UTC |

**Relacionamentos**:
- 1:1 com `Servico`

---

### 4.4 `MensagemServico`

**Responsabilidade**: Representa cada mensagem do chat vinculado a um serviço. Suporta texto e imagens com moderação.

**Tabela**: `mensagens_servico`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `servico_id` | FK → `services.id` |
| `RemetenteId` | `Guid?` | `remetente_id` | FK → `users.id`, nulo para msgs de sistema |
| `PapelRemetente` | `PapelRemetente` | `papel_remetente` | `cliente` / `prestador` / `admin` / `sistema` |
| `TipoMensagem` | `TipoMensagem` | `tipo_mensagem` | `texto` / `imagem` / `proposta` / `sistema` |
| `Conteudo` | `string` | `conteudo` | Texto ou URL Cloudinary moderada |
| `ValorProposta` | `decimal?` | `valor_proposta` | Preenchido quando `TipoMensagem = Proposta` |
| `StatusProposta` | `StatusProposta?` | `status_proposta` | `pendente` / `aceita` / `recusada` / `expirada` |
| `ImagemModerada` | `bool` | `imagem_moderada` | True após aprovação da moderação |
| `ImagemAprovada` | `bool?` | `imagem_aprovada` | Resultado da moderação |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |

**Relacionamentos**:
- N:1 com `Servico`
- N:1 com `Usuario` (remetente)

---

### 4.5 `DadosBancarios`

**Responsabilidade**: Dados PIX e bancários do Prestador para recebimento de repasses.

**Tabela**: `dados_bancarios`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `usuario_id` | FK único → `users.id` |
| `TipoChavePix` | `TipoChavePix` | `tipo_chave_pix` | `cpf`/`cnpj`/`email`/`telefone`/`aleatoria` |
| `ChavePix` | `string` | `chave_pix` | Valor da chave |
| `NomeCompleto` | `string` | `nome_completo` | Nome completo para TED/DOC |
| `CpfCnpj` | `string` | `cpf_cnpj` | Documento do titular |
| `NomeBanco` | `string?` | `nome_banco` | Nome do banco |
| `Agencia` | `string?` | `agencia` | Agência bancária |
| `NumeroConta` | `string?` | `numero_conta` | Conta bancária |
| `TipoConta` | `string?` | `tipo_conta` | Corrente/Poupança |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `AtualizadoEm` | `DateTime` | `atualizado_em` | UTC |

**Relacionamentos**:
- 1:1 com `Usuario`

---

### 4.6 `Avaliacao`

**Responsabilidade**: Avaliação bilateral após conclusão do serviço. Cada parte avalia a outra separadamente.

**Tabela**: `avaliacoes`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `servico_id` | FK → `services.id` |
| `AvaliadorId` | `Guid` | `avaliador_id` | FK → `users.id` |
| `AvaliadoId` | `Guid` | `avaliado_id` | FK → `users.id` |
| `Nota` | `int` | `nota` | 1 a 5 (constraint check) |
| `Comentario` | `string?` | `comentario` | Opcional, max 1000 caracteres |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |

**Constraint**: Unique(`ServicoId`, `AvaliadorId`) — uma avaliação por pessoa por serviço.

**Relacionamentos**:
- N:1 com `Servico`
- N:1 com `Usuario` (avaliador)
- N:1 com `Usuario` (avaliado)

---

### 4.7 `CategoriaUsuario`

**Responsabilidade**: Relação muitos-para-muitos entre Prestador e suas categorias de serviço.

**Tabela**: `usuarios_categorias`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `UsuarioId` | `Guid` | `usuario_id` | FK → `users.id` |
| `CategoriaId` | `Guid` | `categoria_id` | FK → `categories.id` (nunca string livre) |

**PK composta**: (`usuario_id`, `categoria_id`)

---

### 4.8 `CidadeUsuario`

**Responsabilidade**: Relação muitos-para-muitos entre Prestador e cidades onde atua.

**Tabela**: `usuarios_cidades`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `UsuarioId` | `Guid` | `usuario_id` | FK → `users.id` |
| `CidadeId` | `Guid` | `cidade_id` | FK → `cities.id` (substituiu texto livre) |

**PK composta**: (`usuario_id`, `cidade_id`)

---

### 4.9 `ImagemPortfolio`

**Responsabilidade**: Imagens de trabalhos anteriores do Prestador hospedadas no Cloudinary.

**Tabela**: `imagens_portfolio`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `usuario_id` | FK → `users.id` |
| `Url` | `string` | `url` | URL pública Cloudinary |
| `CloudinaryPublicId` | `string` | `cloudinary_public_id` | Para deleção |
| `Moderada` | `bool` | `moderado` | Default false |
| `Aprovada` | `bool?` | `aprovado` | Null = pendente, true = ok, false = rejeitada |
| `Ordem` | `int` | `ordem_exibicao` | Ordena exibição |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `DeletadoEm` | `DateTime?` | `deletado_em` | UTC — soft delete; filtro global EF Core |

---

### 4.10 `Categoria`

**Responsabilidade**: Catálogo canônico de categorias de serviço. Todas as referências a categoria usam FK para esta tabela — nunca string livre.

**Tabela**: `categorias`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `nome` | Ex: "Encanador", "Eletricista" |
| `Slug` | `string` | `slug` | Único, kebab-case. Ex: `encanador` |
| `Ativa` | `bool` | `ativo` | Categorias inativas não aparecem na busca |
| `Ordem` | `int` | `ordem_exibicao` | Ordena exibição no frontend |

**Constraint**: `UNIQUE(slug)`

---

### 4.11 `Cidade`

**Responsabilidade**: Catálogo de cidades cobertas pela plataforma. Todas as referências a cidade usam FK para esta tabela.

**Tabela**: `cidades`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `nome` | Ex: "Itapevi" |
| `Estado` | `string` | `estado` | Sigla UF. Ex: "SP" |
| `Slug` | `string` | `slug` | Único, kebab-case. Ex: `itapevi` |
| `Ativa` | `bool` | `ativo` | Cidades inativas não aparecem na busca |

**Constraint**: `UNIQUE(slug)`

---

### 4.12 `RefreshToken`

**Responsabilidade**: Armazena tokens de renovação de sessão. Cada login emite um par (Access Token 15 min + Refresh Token 30 dias). A rotação obrigatória invalida o token anterior a cada renovação.

**Tabela**: `tokens_renovacao`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `usuario_id` | FK → `users.id` |
| `Token` | `string` | `token` | Hash SHA-256 do token (nunca o valor bruto) |
| `ExpiracaoEm` | `DateTime` | `expira_em` | UTC; padrão `now + 30 dias` |
| `RevogadoEm` | `DateTime?` | `revogado_em` | UTC; preenchido ao revogar/rotacionar |
| `SubstituidoPor` | `string?` | `substituido_por` | Hash do novo token (rastreabilidade de rotação) |
| `Ip` | `string?` | `endereco_ip` | IP do login original |
| `UserAgent` | `string?` | `user_agent` | Device fingerprint |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |

**Regras**:
- Ao renovar: token atual → `RevogadoEm = now` + `SubstituidoPor = novoTokenHash`; novo token emitido
- Ao logout: token → `RevogadoEm = now`
- Token expirado ou revogado deve retornar 401

**Relacionamentos**:
- N:1 com `Usuario`

---

### 4.13 `AuditLog`

**Responsabilidade**: Trilha de auditoria imutável. Registra ações críticas com identificação do ator, entidade afetada e contexto de rede. Registros **nunca são deletados**.

**Tabela**: `logs_auditoria`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid?` | `usuario_id` | FK → `users.id`; nulo para ações de sistema/job |
| `Acao` | `string` | `acao` | Ex: `usuario.login`, `servico.criado`, `pagamento.liberado` |
| `Entidade` | `string` | `entidade` | Nome da entidade afetada. Ex: `Servico`, `Cobranca` |
| `EntidadeId` | `string?` | `entidade_id` | ID da entidade afetada (string para suportar Guid e outros) |
| `Ip` | `string?` | `endereco_ip` | IP da requisição |
| `UserAgent` | `string?` | `user_agent` | Header User-Agent |
| `Detalhes` | `string?` | `detalhes` | JSON com contexto adicional (campos alterados, motivo) |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |

**Ações obrigatórias a registrar**:
`usuario.login`, `usuario.logout`, `usuario.cadastro`, `usuario.perfil_alterado`, `dados_bancarios.cadastrado`, `servico.criado`, `proposta.aceita`, `pagamento.confirmado`, `pagamento.liberado`, `servico.cancelado`, `disputa.aberta`, `disputa.resolvida`, `admin.*` (todas as ações administrativas)

**Relacionamentos**:
- N:1 com `Usuario` (nullable)

---

### 4.14 `Disputa`

**Responsabilidade**: Contestação aberta pelo Cliente quando discorda da conclusão marcada pelo Prestador. Enquanto disputa está ativa, o pagamento permanece retido e o Admin decide o resultado.

**Tabela**: `disputas`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `ServicoId` | `Guid` | `servico_id` | FK único → `services.id`; um serviço tem no máximo uma disputa |
| `AbertaPorId` | `Guid` | `aberto_por_id` | FK → `users.id`; deve ser o cliente |
| `Motivo` | `string` | `motivo` | Motivo selecionado pelo cliente |
| `Descricao` | `string?` | `descricao` | Descrição detalhada (opcional) |
| `Status` | `StatusDisputa` | `status` | Ver enum abaixo |
| `ResolvidaPorId` | `Guid?` | `resolvido_por_id` | FK → `users.id`; admin que resolveu |
| `DecisaoAdmin` | `string?` | `decisao_admin` | Justificativa da decisão do admin |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |
| `ResolvidoEm` | `DateTime?` | `resolvido_em` | UTC |

**Enum `StatusDisputa`**: `Aberta`, `EmAnalise`, `ResolvidaCliente`, `ResolvidaPrestador`

**Relacionamentos**:
- N:1 com `Servico`
- N:1 com `Usuario` (quem abriu)
- N:1 com `Usuario` (admin que resolveu)

---

### 4.15 `Notificacao`

**Responsabilidade**: Notificações in-app enviadas ao usuário sobre eventos relevantes. Lidas via polling ou SignalR (V2).

**Tabela**: `notificacoes`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `usuario_id` | FK → `users.id` |
| `Titulo` | `string` | `titulo` | Ex: "Nova proposta recebida" |
| `Mensagem` | `string` | `mensagem` | Texto completo da notificação |
| `Lida` | `bool` | `lido` | Default `false` |
| `Tipo` | `string` | `tipo` | Ex: `proposta`, `pagamento`, `disputa`, `avaliacao` |
| `ReferenciaId` | `string?` | `referencia_id` | ID da entidade relacionada (ServicoId, DisputaId, etc.) |
| `CriadoEm` | `DateTime` | `criado_em` | UTC |

**Eventos que geram notificação**: nova proposta recebida, contraproposta, pagamento confirmado, serviço concluído (por prestador), conclusão automática, disputa aberta, disputa resolvida, avaliação recebida.

**Relacionamentos**:
- N:1 com `Usuario`

---

## 5. Casos de Uso por Ator

### 5.1 Cliente

| ID | Caso de Uso | Pré-condição | Resultado |
|----|------------|--------------|----------|
| CL-01 | Criar conta como cliente | Email não cadastrado | Conta criada, Access Token + Refresh Token emitidos |
| CL-02 | Fazer login | Conta existente | Access Token (15 min) + Refresh Token (30 dias) emitidos |
| CL-03 | Completar perfil (foto) | Autenticado | Foto enviada ao Cloudinary, URL salva |
| CL-04 | Buscar prestadores por categoria e cidade | Público | Lista paginada de prestadores |
| CL-05 | Ver perfil público do prestador | Público | Página pública com slug na URL |
| CL-06 | Criar solicitação de serviço | Autenticado | Serviço criado em `EmNegociacao` |
| CL-07 | Contrapropor preço | Serviço em `EmNegociacao` | Mensagem de proposta criada |
| CL-08 | Aceitar proposta do prestador | Serviço em `EmNegociacao` | Proposta aceita, Serviço avança para `AguardandoPagamento` |
| CL-09 | Ver QR Code PIX | Serviço em `AguardandoPagamento` | Dados da cobrança com PIX |
| CL-10 | Confirmar conclusão do serviço | Serviço em `AguardandoConfirmacaoCliente` | Serviço → `Concluido`, pagamento liberado |
| CL-11 | Avaliar prestador | Serviço `Concluido`, avaliação pendente | Avaliação registrada |
| CL-12 | Ver histórico de serviços | Autenticado | Lista dos próprios serviços |
| CL-13 | Enviar mensagem no chat | Serviço ativo | Mensagem criada |
| CL-14 | Enviar imagem no chat | Serviço ativo | Upload para Cloudinary + moderação |
| CL-15 | Renovar sessão (refresh) | Refresh Token válido e não revogado | Novo Access Token + novo Refresh Token (rotação) |
| CL-16 | Logout | Autenticado | Refresh Token atual revogado no banco |
| CL-17 | Abrir disputa | Serviço em `AguardandoConfirmacaoCliente` | `Disputa` criada, Serviço → `EmDisputa`, pagamento permanece retido |

### 5.2 Prestador

| ID | Caso de Uso | Pré-condição | Resultado |
|----|------------|--------------|----------|
| PR-01 | Criar conta como prestador | Email não cadastrado | Conta criada, Access Token + Refresh Token emitidos |
| PR-02 | Completar perfil (CPF, foto, categorias, cidades, bio) | Autenticado prestador | Slug gerado, perfil público disponível |
| PR-03 | Adicionar imagens ao portfólio | Autenticado prestador | Upload Cloudinary + fila de moderação |
| PR-04 | Cadastrar dados bancários (PIX) | Autenticado prestador | `DadosBancarios` salvo |
| PR-05 | Ver solicitações disponíveis | Autenticado prestador | Solicitações em `EmNegociacao` sem prestador vinculado, filtradas por categoria/cidade |
| PR-06 | Vincular-se a uma solicitação | Serviço sem prestador | Prestador vinculado, negociação inicia |
| PR-07 | Enviar proposta de preço | Serviço em `EmNegociacao` | Mensagem de proposta criada |
| PR-08 | Contrapropor preço | Serviço em `EmNegociacao` | Mensagem de proposta criada |
| PR-09 | Aceitar proposta do cliente | Serviço em `EmNegociacao` | Proposta aceita, Serviço avança para `AguardandoPagamento` |
| PR-10 | Marcar serviço como concluído | Serviço em `EmAndamento` | Serviço → `AguardandoConfirmacaoCliente` |
| PR-11 | Avaliar cliente | Serviço `Concluido`, avaliação pendente | Avaliação registrada |
| PR-12 | Ver extrato de repasses | Autenticado prestador | Cobranças com status `Liberado` |
| PR-13 | Enviar mensagem no chat | Serviço ativo | Mensagem criada |
| PR-14 | Enviar imagem no chat | Serviço ativo | Upload para Cloudinary + moderação |
| PR-15 | Renovar sessão (refresh) | Refresh Token válido | Novo Access Token + novo Refresh Token (rotação) |
| PR-16 | Logout | Autenticado | Refresh Token atual revogado no banco |

### 5.3 Admin

| ID | Caso de Uso | Pré-condição |
|----|------------|--------------|
| AD-01 | Ver estatísticas gerais | `Papel = Admin` |
| AD-02 | Listar usuários | `Papel = Admin` |
| AD-03 | Listar serviços com status | `Papel = Admin` |
| AD-04 | Alterar status de serviço manualmente | `Papel = Admin` |
| AD-05 | Ler chat de qualquer serviço | `Papel = Admin` |
| AD-06 | Enviar mensagem como plataforma | `Papel = Admin` |
| AD-07 | Listar cobranças | `Papel = Admin` |
| AD-08 | Moderar imagens de portfólio pendentes | `Papel = Admin` |
| AD-09 | Bloquear/desbloquear usuário | `Papel = Admin` |
| AD-10 | Ver extrato financeiro da plataforma | `Papel = Admin` |
| AD-11 | Resolver disputa | `Papel = Admin`, disputa em `Aberta` ou `EmAnalise` |
| AD-12 | Consultar trilha de auditoria | `Papel = Admin` |

### 5.4 Sistema (Jobs Automatizados)

| ID | Caso de Uso | Gatilho |
|----|------------|---------|
| SY-01 | Auto-conclusão por inatividade | 7 dias após `AguardandoConfirmacaoCliente` sem resposta |
| SY-02 | Expiração de PIX | Após `PixExpiracaoEm` com status `AguardandoPagamento` |
| SY-03 | Processar webhook de pagamento | Webhook recebido da Pagar.me |
| SY-04 | Liberar pagamento ao prestador | Serviço `Concluido` → transferência PIX |
| SY-05 | Moderar imagens assíncronas | Imagem enviada para Cloudinary |
| SY-06 | Limpar arquivos Cloudinary órfãos | Imagem rejeitada ou usuário deletado |
| SY-07 | Regenerar médias de avaliação | Nova avaliação registrada |
| SY-08 | Limpar Refresh Tokens expirados | Diário (job de manutenção) |
| SY-09 | Notificar usuários sobre eventos | Handler de evento de domínio |

---

## 6. Fluxos Principais

### 6.1 Fluxo Completo de Serviço

```
[CLIENTE]                     [SISTEMA]                    [PRESTADOR]
    │                              │                              │
    ├──Cria solicitação────────────▶                              │
    │                         Serviço: EmNegociacao              │
    │                         (sem prestador)                     │
    │                              │◀─────────Vincula-se─────────┤
    │                              │                              │
    │◀──────────────────────Envia proposta (R$ X)────────────────┤
    │                              │                              │
    ├──Contrapropoõe (R$ Y)────────▶                              │
    │                              ├────────────────────────────▶│
    │                              │                              │
    │◀──────────────────────Aceita (R$ Y)────────────────────────┤
    │                         Acordo: Y                           │
    │                         Serviço: AguardandoPagamento        │
    │                         Cobranca: Pendente                  │
    │                         PIX gerado                         │
    │                              │                              │
    ├──Paga PIX────────────────────▶                              │
    │                    Webhook Pagar.me recebido                │
    │                    Cobranca: Pago → Retido                  │
    │                    Serviço: Pago → EmAndamento              │
    │                              │                              │
    │                              │◀─────────Executa serviço────┤
    │                              │◀─────────Marca concluído────┤
    │                    Serviço: AguardandoConfirmacaoCliente    │
    │                    AguardandoConfirmacaoDesde = now         │
    │                              │                              │
    ├──Confirma────────────────────▶                              │
    │                    Serviço: Concluido                       │
    │                    Cobranca: Liberado                       │
    │                    Repasse PIX ao prestador                 │
    │                              │                              │
    ├──Avalia prestador────────────▶◀─────────Avalia cliente──────┤
    │                              │                              │
```

**Caminho alternativo — sem resposta do cliente em 7 dias:**

```
    AguardandoConfirmacaoCliente
           │  (7 dias sem resposta)
           ▼
    [JOB] Verificação diária
           │
           ▼
    Serviço: Concluido (automático)
    Cobranca: Liberado (automático)
    Repasse ao prestador
    Notificação ao cliente
```

### 6.2 Fluxo de Negociação (Máquina de Propostas)

```
Serviço EmNegociacao
        │
        ├── Prestador envia proposta (valor P1)
        │       └── StatusProposta: Pendente
        │
        ├── Cliente aceita P1
        │       └── StatusProposta: Aceita
        │           Serviço → AguardandoPagamento
        │
        ├── Cliente recusa / contrapropoõe (valor C1)
        │       └── P1 → StatusProposta: Recusada
        │           Nova proposta C1: Pendente
        │
        └── Ciclo continua até acordo ou cancelamento
```

**Regra**: Somente a proposta mais recente pode estar com `StatusProposta = Pendente`. Ao criar nova proposta, a anterior deve ter seu status atualizado para `Expirada`.

### 6.3 Fluxo de Pagamento (Pagar.me)

```
[BACKEND]                    [PAGAR.ME]                   [WEBHOOK]
    │                              │                           │
    ├──Cria Order (PIX)───────────▶│                           │
    │◀──Order criada (QR Code)─────┤                           │
    │                              │                           │
    │  Armazena PIX na Cobrança    │                           │
    │  Status: AguardandoPagamento │                           │
    │                              │                           │
    │                    [Cliente paga no banco]               │
    │                              │                           │
    │                              ├──POST /webhooks/pagarme──▶│
    │                              │                   Verificar assinatura HMAC
    │                              │                   Atualizar Cobranca
    │                              │                   Atualizar Servico
    │                              │                           │
    │◀─────────────────────────────────────────────────────────┤
    │  Cobranca: Pago → Retido                                 │
    │  Servico: Pago → EmAndamento                             │
```

### 6.4 Fluxo de Avaliação

```
Serviço Concluido
        │
        ├── Notificação enviada para Cliente e Prestador
        │
        ├── Cliente avalia Prestador (1-5 + comentário)
        │       Avaliacao { AvaliadorId=cliente, AvaliadoId=prestador }
        │
        ├── Prestador avalia Cliente (1-5 + comentário)
        │       Avaliacao { AvaliadorId=prestador, AvaliadoId=cliente }
        │
        └── Job recalcula MediaAvaliacoes do Usuario avaliado
```

**Janela de avaliação**: 30 dias após conclusão. Após esse prazo, a avaliação não pode mais ser registrada.

---

### 6.5 Fluxo de Disputa

```
[PRESTADOR]                   [SISTEMA]                    [CLIENTE]
     │                              │                           │
     ├──Marca concluído─────────────▶                           │
     │                    Serviço: AguardandoConfirmacaoCliente │
     │                    Notificação enviada ao cliente        │
     │                              │                           │
     │                              │◀──────────────────────────┤
     │                              │          (opção A) Confirma
     │                    Serviço: Concluido                    │
     │                    Cobranca: Liberado                    │
     │                              │                           │
     │                              │◀──────────────────────────┤
     │                              │          (opção B) Abre disputa
     │                    Serviço: EmDisputa                    │
     │                    Cobranca: permanece Retido            │
     │                    Disputa criada (Motivo + Descrição)   │
     │                              │                           │
     │                        [ADMIN analisa]                   │
     │                              │                           │
     │                    ┌─────────┴──────────┐               │
     │              Favor prestador       Favor cliente         │
     │                    │                    │                │
     │             Serviço: Concluido    Serviço: Concluido     │
     │             Cobranca: Liberado    Cobranca: Reembolsado  │
     │             (split 80/20)         (100% devolvido)       │
     │                              │                           │
```

**Regras da disputa**:
- Somente o Cliente pode abrir disputa, a partir do estado `AguardandoConfirmacaoCliente`
- Um serviço pode ter no máximo **uma** disputa ativa
- Enquanto `EmDisputa`: nenhuma das partes pode alterar o serviço
- O Admin deve registrar `DecisaoAdmin` (justificativa textual) ao resolver
- Resultado da disputa gera `AuditLog` obrigatório

---

## 7. Máquina de Estados

### 7.1 `StatusServico`

```
                    ┌─────────────────┐
                    │  EmNegociacao   │◀─── Criação pelo Cliente
                    └────────┬────────┘
                             │ Acordo de preço aceito por ambas as partes
                             ▼
                    ┌─────────────────┐
                    │AguardandoPagamento│◀─── Cobranca criada, PIX gerado
                    └────────┬────────┘
                             │ Webhook pagamento confirmado (Pagar.me)
                             ▼
                    ┌─────────────────┐
                    │      Pago       │ Cobranca: Pago → Retido
                    └────────┬────────┘
                             │ (transição automática imediata)
                             ▼
                    ┌─────────────────┐
                    │  EmAndamento    │◀─── Prestador executando
                    └────────┬────────┘
                             │ Prestador marca "concluído"
                             ▼
                    ┌─────────────────────────────┐
                    │ AguardandoConfirmacaoCliente │
                    └────────────┬────────────────┘
                        │        │               │
          Cliente confirma  7 dias sem resp   Cliente abre disputa
                        │        │               │
                        ▼        ▼               ▼
                                          ┌─────────────┐
                                          │  EmDisputa  │ Cobranca: Retido
                                          └──────┬──────┘
                                                 │ Admin decide
                                         ┌───────┴───────┐
                                         ▼               ▼
                                   Favor cliente   Favor prestador
                                         │               │
                    ┌────────────────────┘               │
                    ▼                                     ▼
              Cobranca: Reembolsado              Cobranca: Liberado
                    │                                     │
                    └──────────────┬──────────────────────┘
                                   ▼
                    ┌─────────────────────────────┐
                    │          Concluido          │
                    └─────────────────────────────┘

 De qualquer estado (exceto Concluido):
        │
        └──── Admin ou acordo de cancelamento ──▶ Cancelado
                                                  Cobranca: Reembolsado (se Pago)
```

**Tabela de transições válidas:**

| De | Para | Gatilho | Ator |
|----|------|---------|------|
| `EmNegociacao` | `AguardandoPagamento` | Proposta aceita por ambas as partes | Sistema |
| `EmNegociacao` | `Cancelado` | Qualquer parte cancela / Admin | Cliente / Prestador / Admin |
| `AguardandoPagamento` | `Pago` | Webhook PIX confirmado | Sistema (webhook) |
| `AguardandoPagamento` | `Cancelado` | PIX expirado sem pagamento | Sistema (job) |
| `Pago` | `EmAndamento` | Imediato após confirmação do pagamento | Sistema |
| `EmAndamento` | `AguardandoConfirmacaoCliente` | Prestador marca serviço como concluído | Prestador |
| `EmAndamento` | `Cancelado` | Admin | Admin |
| `AguardandoConfirmacaoCliente` | `Concluido` | Cliente confirma | Cliente |
| `AguardandoConfirmacaoCliente` | `Concluido` | 7 dias sem resposta | Sistema (job) |
| `AguardandoConfirmacaoCliente` | `EmDisputa` | Cliente abre disputa | Cliente |
| `AguardandoConfirmacaoCliente` | `Cancelado` | Admin (casos excepcionais) | Admin |
| `EmDisputa` | `Concluido` | Admin decide em favor do prestador | Admin |
| `EmDisputa` | `Concluido` | Admin decide em favor do cliente (com reembolso) | Admin |
| `EmDisputa` | `Cancelado` | Admin (casos excepcionais) | Admin |

---

### 7.2 `StatusCobranca`

```
              ┌────────────┐
              │  Pendente  │◀─── Criada ao acordar preço
              └─────┬──────┘
                    │ PIX confirmado (webhook)
                    ▼
              ┌────────────┐
              │    Pago    │ Pagamento recebido pela plataforma
              └─────┬──────┘
                    │ Automaticamente retido
                    ▼
              ┌────────────┐
              │   Retido   │ Aguardando conclusão do serviço
              └─────┬──────┘
                    │ Serviço Concluido
                    ▼
              ┌────────────┐
              │  Liberado  │ Repasse ao prestador executado
              └────────────┘

 De Pendente ou Pago:
              └──── Admin ──▶ Cancelado
 De Retido:
              └──── Admin ──▶ Reembolsado → Devolução ao cliente
```

**Tabela de transições válidas:**

| De | Para | Gatilho |
|----|------|---------|
| `Pendente` | `Pago` | Webhook Pagar.me |
| `Pendente` | `Cancelado` | PIX expirado / Admin |
| `Pago` | `Retido` | Imediato (mesmo webhook) |
| `Retido` | `Liberado` | Serviço `Concluido` |
| `Retido` | `Reembolsado` | Admin (disputa / fraude) |

---

## 8. Eventos de Domínio

Todos os eventos são publicados pelo domínio e consumidos pela camada de Application (handlers). A implementação inicial pode ser in-process com `MediatR`. Migração para mensageria (RabbitMQ/Azure Service Bus) é considerada no futuro.

| Evento | Publicado por | Payload resumido |
|--------|--------------|-----------------|
| `UsuarioCadastrado` | `ServicoAutenticacao.CadastrarAsync` | `{ UsuarioId, Email, TipoConta }` |
| `PerfilPrestadorCompleto` | `ServicoPerfilPrestador` | `{ PrestadorId, Slug, CategoriaSlug, CidadeSlug }` |
| `SolicitacaoCriada` | `ServicoServico.CriarSolicitacaoAsync` | `{ ServicoId, ClienteId, Categoria, Cidade }` |
| `PrestadorVinculado` | `ServicoServico.VincularPrestadorAsync` | `{ ServicoId, PrestadorId }` |
| `PropostaEnviada` | `ServicoNegociacao.EnviarPropostaAsync` | `{ ServicoId, MensagemId, Valor, PapelRemetente }` |
| `PropostaAceita` | `ServicoNegociacao.AceitarPropostaAsync` | `{ ServicoId, MensagemId, ValorAcordado }` |
| `AcordoFirmado` | `ServicoNegociacao` | `{ ServicoId, ValorFinal, ClienteId, PrestadorId }` |
| `CobrancaCriada` | `ServicoFinanceiro.CriarCobrancaAsync` | `{ CobrancaId, ServicoId, ValorTotal, PixExpiracaoEm }` |
| `PixGerado` | `ServicoFinanceiro.GerarPixAsync` | `{ CobrancaId, QrCode, CopiaCola, ExpiracaoEm }` |
| `PagamentoConfirmado` | `ServicoWebhook.ProcessarAsync` | `{ CobrancaId, ServicoId, ValorPago, PagadoEm }` |
| `ServicoEmAndamento` | `ServicoServico` | `{ ServicoId, ClienteId, PrestadorId }` |
| `ServicoMarcadoConcluido` | `ServicoServico.MarcarConcluidoAsync` | `{ ServicoId, PrestadorId, MarcadoEm }` |
| `ServicoConfirmadoCliente` | `ServicoServico.ConfirmarConclusaoAsync` | `{ ServicoId, ClienteId, ConfirmadoEm }` |
| `ServicoConcluido` | `ServicoServico` | `{ ServicoId, ClienteId, PrestadorId, ValorPrestador }` |
| `ServicoConcluido_AutomaticoPorInatividade` | `JobConclusaoAutomatica` | `{ ServicoId, ConcluidoEm }` |
| `PagamentoLiberado` | `ServicoFinanceiro.LiberarPagamentoAsync` | `{ CobrancaId, ServicoId, PrestadorId, ValorPrestador }` |
| `ServicoCancelado` | `ServicoServico.CancelarAsync` | `{ ServicoId, MotivoCancelamento, AtorId }` |
| `ReembolsoIniciado` | `ServicoFinanceiro.ReembolsarAsync` | `{ CobrancaId, ServicoId, ClienteId, ValorTotal }` |
| `AvaliacaoRegistrada` | `ServicoAvaliacao.AvaliarAsync` | `{ AvaliacaoId, ServicoId, AvaliadorId, AvaliadoId, Nota }` |
| `MediaAvaliacaoAtualizada` | `JobMediaAvaliacao` | `{ UsuarioId, NovaMedia, TotalAvaliacoes }` |
| `ImagemSubmetidaModeracao` | `ServicoCloudinary` | `{ Referencia, TipoReferencia (chat/portfolio), CloudinaryPublicId }` |
| `ImagemAprovada` | `ServicoModeracao.ProcessarResultadoAsync` | `{ CloudinaryPublicId, Referencia }` |
| `ImagemRejeitada` | `ServicoModeracao.ProcessarResultadoAsync` | `{ CloudinaryPublicId, Referencia }` |
| `PixExpirado` | `JobExpiracaoPix` | `{ CobrancaId, ServicoId }` |
| `DisputaAberta` | `ServicoDisputa.AbrirDisputaAsync` | `{ DisputaId, ServicoId, ClienteId, Motivo }` |
| `DisputaEmAnalise` | `ServicoDisputa.IniciarAnaliseAsync` | `{ DisputaId, AdminId }` |
| `DisputaResolvida` | `ServicoDisputa.ResolverAsync` | `{ DisputaId, ServicoId, Resultado, AdminId }` |
| `RefreshTokenRevogado` | `ServicoAutenticacao.LogoutAsync` | `{ UsuarioId, TokenHash }` |
| `NotificacaoCriada` | `ServicoNotificacao.NotificarAsync` | `{ NotificacaoId, UsuarioId, Tipo }` |

---

## 9. Jobs Automatizados

Implementar com `IHostedService` + timer periódico (Quartz.NET é alternativa mais robusta para produção).

### Job 1 — Conclusão Automática por Inatividade

| Atributo | Valor |
|---------|-------|
| **Nome** | `JobConclusaoAutomatica` |
| **Frequência** | A cada 1 hora |
| **Lógica** | Busca serviços com `Status = AguardandoConfirmacaoCliente` e `AguardandoConfirmacaoDesde < UtcNow - 7 dias` |
| **Ação** | Transicionar para `Concluido`, publicar `ServicoConcluido_AutomaticoPorInatividade`, liberar pagamento |
| **Idempotência** | Verificar status antes de alterar; usar transação de banco |

### Job 2 — Expiração de PIX

| Atributo | Valor |
|---------|-------|
| **Nome** | `JobExpiracaoPix` |
| **Frequência** | A cada 15 minutos |
| **Lógica** | Busca cobranças com `Status = Pendente` e `PixExpiracaoEm < UtcNow` |
| **Ação** | Cancelar cobrança, cancelar serviço (se ainda em `AguardandoPagamento`), publicar `PixExpirado` |

### Job 3 — Liberação de Pagamento ao Prestador

| Atributo | Valor |
|---------|-------|
| **Nome** | `JobLiberacaoPagamento` |
| **Gatilho** | Handler do evento `ServicoConcluido` |
| **Lógica** | Chamar `IProcessadorPagamento.TransferirAsync()` com dados PIX do prestador |
| **Ação** | Atualizar `Cobranca.Status = Liberado`, preencher `LiberadoEm` |
| **Retry** | Máximo 3 tentativas com backoff exponencial |

### Job 4 — Limpeza de Arquivos Cloudinary Órfãos

| Atributo | Valor |
|---------|-------|
| **Nome** | `JobLimpezaCloudinary` |
| **Frequência** | Diário (madrugada) |
| **Lógica** | Busca imagens com `Aprovada = false` criadas há mais de 7 dias; imagens de usuários deletados |
| **Ação** | Chamar `IArmazenamentoArquivo.DeletarAsync()`, remover registros do banco |

### Job 5 — Recalcular Médias de Avaliação

| Atributo | Valor |
|---------|-------|
| **Nome** | `JobMediaAvaliacao` |
| **Gatilho** | Handler do evento `AvaliacaoRegistrada` |
| **Lógica** | `AVG(Nota)` e `COUNT(*)` para o usuário avaliado |
| **Ação** | Atualizar `Usuario.MediaAvaliacoes` e `Usuario.TotalAvaliacoes` |

### Job 6 — Processamento de Webhook Pagar.me

| Atributo | Valor |
|---------|-------|
| **Nome** | `ServicoWebhook` (síncrono via endpoint) |
| **Gatilho** | POST `/webhooks/pagarme` |
| **Lógica** | Verificar assinatura HMAC-SHA256, identificar evento (`payment.confirmed`, `payment.failed`), processar |
| **Idempotência** | Armazenar `PagarmeOrderId` e verificar processamento duplicado |

---

## 10. Segurança

### 10.1 Autenticação

**Access Token (JWT HS256)**
- Expiração: **15 minutos**
- Segredo via variável de ambiente `SESSION_SECRET` (nunca hardcoded em produção)
- Claims: `userId`, `email`, `accountType`, `papel`
- Transmitido via header `Authorization: Bearer <token>`
- Stateless — não armazenado no banco

**Refresh Token**
- Expiração: **30 dias**
- Gerado no login e na renovação; armazenado no banco como **hash SHA-256** (nunca o valor bruto)
- **Rotação obrigatória**: cada uso invalida o token atual e emite um novo
- **Revogável**: logout, suspeita de comprometimento ou admin podem revogar via `RevogadoEm`
- Transmitido via cookie `HttpOnly; Secure; SameSite=Strict` (não acessível pelo JavaScript)
- Endpoint exclusivo: `POST /api/auth/refresh`

**Fluxo de renovação**:
1. Frontend detecta resposta 401
2. Faz `POST /api/auth/refresh` com o cookie Refresh Token
3. Backend valida hash, verifica `ExpiracaoEm` e `RevogadoEm`
4. Emite novo Access Token + novo Refresh Token (rotação); invalida o anterior
5. Frontend retenta a requisição original com novo Access Token

**Justificativa**: Marketplace financeiro não deve usar JWT de longa duração sem renovação. Access Token de 15 min limita a janela de exposição em caso de vazamento. Refresh Token revogável permite invalidar sessões comprometidas.

### 10.2 Autorização

| Recurso | Regra |
|---------|-------|
| `/api/admin/*` | `[Authorize(Roles = "admin")]` |
| `/api/servicos/*` | `[Authorize]` + validação de propriedade (cliente ou prestador do serviço) |
| `/api/chat/*` | `[Authorize]` + deve ser participante do serviço |
| `/webhooks/pagarme` | Sem JWT, mas com verificação de assinatura HMAC |
| Perfis públicos `/.../{slug}` | Sem autenticação |

### 10.3 Proteção de Uploads (Cloudinary)

- Upload via signed URL gerado pelo backend (nunca expor API key ao frontend)
- Moderação automática habilitada na conta Cloudinary (Rekognition / Moderation Add-on)
- Conteúdo explícito → `ImagemRejeitada` → arquivo deletado do Cloudinary
- Tamanho máximo: 10 MB por imagem
- Tipos permitidos: `image/jpeg`, `image/png`, `image/webp`
- Imagens do chat exibidas com overlay de loading até aprovação

### 10.4 Proteção de Webhooks

- Validação de assinatura HMAC-SHA256 da Pagar.me em todo request
- Chave de webhook em variável de ambiente `PAGARME_WEBHOOK_SECRET`
- Endpoint responde 200 imediatamente e processa de forma assíncrona (evita timeout)
- Idempotência: verificar se `PagarmeOrderId` já foi processado antes de alterar estados

### 10.5 Rate Limiting

| Endpoint | Limite |
|---------|--------|
| `POST /api/auth/login` | 10 req / minuto por IP |
| `POST /api/auth/register` | 5 req / minuto por IP |
| `POST /api/chat/{id}/mensagens` | 30 req / minuto por usuário |
| Geral | 200 req / minuto por IP |

Implementar com `Microsoft.AspNetCore.RateLimiting` (disponível no .NET 7+).

### 10.6 Proteção de Dados (LGPD)

- CPF armazenado criptografado no banco (AES-256) — nunca retornado pela API pública
- Dados bancários (chave Pix, CPF/CNPJ) acessíveis apenas pelo próprio prestador e admin
- Perfis públicos de prestadores: exibir apenas Nome, Foto, Especialidade, Cidade, Nota média
- Direito de exclusão: endpoint `DELETE /api/minha-conta` anonimiza dados pessoais (não deleta registros financeiros por obrigação legal)
- Logs de acesso retidos por 90 dias máximo
- Consentimento explícito na criação de conta (checkbox + timestamp registrado)

### 10.8 Trilha de Auditoria

- Toda ação crítica grava um `AuditLog` de forma **síncrona** antes do commit da transação principal
- O `AuditLog` nunca é deletado — apenas lido por admins e sistemas de monitoramento
- Implementar via interceptor de EF Core ou chamada explícita no `ServicoAuditoria`
- O `Ip` e `UserAgent` são capturados do `HttpContext` e propagados pelo pipeline da requisição
- Ações de jobs (conclusão automática, liberação de pagamento) registram `UsuarioId = null` e `Acao` prefixado com `job.`
- Endpoint `GET /api/admin/audit-logs` com filtros por `acao`, `entidade`, `usuarioId` e intervalo de datas

### 10.7 Outras Proteções

- Todas as rotas de mutação validam que o usuário autenticado é dono do recurso
- Inputs validados com `FluentValidation` antes de atingir a camada de domínio
- Injeção de SQL: impossível via EF Core (parameterized queries)
- XSS: conteúdo de chat deve ser sanitizado antes de renderização no Angular (`DomSanitizer`)
- CORS: apenas origem `https://prontto.com.br` (e `http://localhost:4200` em desenvolvimento)
- HTTPS obrigatório em produção (via NGINX reverse proxy ou Kestrel)

---

## 11. Escalabilidade

### 11.1 Princípios

A arquitetura deve suportar crescimento orgânico sem reescritas. As escolhas foram feitas para escalar **verticalmente primeiro** e **horizontalmente quando necessário**, evitando complexidade prematura.

### 11.2 Banco de Dados

**Índices obrigatórios** (além das PKs):

| Tabela | Colunas | Tipo | Motivo |
|--------|---------|------|--------|
| `usuarios` | `email` | UNIQUE | Login |
| `usuarios` | `slug` | UNIQUE | URL pública |
| `usuarios` | `tipo_conta`, `city_slug`, `category_slug` | Composto | Busca de prestadores |
| `servicos` | `cliente_id` | B-tree | Serviços do cliente |
| `servicos` | `prestador_id` | B-tree | Serviços do prestador |
| `servicos` | `status` | B-tree | Filtros por status |
| `servicos` | `aguardando_confirmacao_desde` | B-tree | Job de auto-conclusão |
| `cobrancas` | `servico_id` | UNIQUE | 1:1 com serviço |
| `cobrancas` | `status`, `pix_expira_em` | Composto | Job de expiração PIX |
| `cobrancas` | `pagarme_order_id` | UNIQUE | Idempotência webhook |
| `mensagens_servico` | `servico_id`, `criado_em` | Composto | Paginação do chat |
| `avaliacoes` | `avaliado_id` | B-tree | Avaliações recebidas |
| `avaliacoes` | `servico_id`, `avaliador_id` | UNIQUE | Constraint de duplicidade |
| `usuarios_categorias` | `categoria_id` | B-tree | Busca por categoria (FK) |
| `usuarios_cidades` | `cidade_id` | B-tree | Busca por cidade (FK) |
| `categorias` | `slug` | UNIQUE | URL pública e busca |
| `cidades` | `slug` | UNIQUE | URL pública e busca |
| `tokens_renovacao` | `token` | UNIQUE | Lookup de renovação |
| `tokens_renovacao` | `usuario_id`, `revogado_em` | Composto | Sessões ativas por usuário |
| `logs_auditoria` | `usuario_id`, `criado_em` | Composto | Auditoria por usuário |
| `logs_auditoria` | `entidade`, `entidade_id` | Composto | Auditoria por entidade |
| `logs_auditoria` | `criado_em` | B-tree | Purge e listagem cronológica |
| `disputas` | `servico_id` | UNIQUE | 1:1 com serviço |
| `disputas` | `status` | B-tree | Filtro de disputas abertas |
| `notificacoes` | `usuario_id`, `lido`, `criado_em` | Composto | Notificações não lidas por usuário |

**Paginação**: todos os endpoints de listagem aceitam `page` e `pageSize` (máximo 50). Usar cursor-based pagination para o chat (`afterId`).

**Pool de conexões**: configurar `Max Pool Size=100` na connection string do EF Core.

### 11.3 Cache

Na V1, cache em memória (`IMemoryCache`) é suficiente. Prioridade:

| Dado | TTL | Estratégia |
|------|-----|-----------|
| Lista de categorias canônicas | 1 hora | In-memory, invalidar ao alterar |
| Perfil público de prestador | 5 minutos | In-memory por slug |
| Estatísticas do admin | 5 minutos | In-memory |

Para V2+ (múltiplas instâncias): migrar para Redis.

### 11.4 Chat em Escala

- V1: polling a cada **10 segundos** no Angular — reduz carga no banco mantendo experiência aceitável
- V2: SignalR para WebSocket real-time (migração sem quebrar API REST existente)
- Paginação do histórico: carregar os últimos 50 mensagens, carregar mais ao scrollar

### 11.5 Uploads

- Upload direto do browser ao Cloudinary via URL assinada (backend assina, browser faz o upload)
- Backend nunca recebe bytes do arquivo, apenas a URL após upload
- Cloudinary faz CDN e otimização automaticamente

### 11.6 Design para Múltiplas Instâncias

- Estado de sessão: stateless (JWT) — sem sessão no servidor
- Jobs: usar `IDistributedLock` (Redis) ao escalar horizontalmente para evitar execução duplicada
- Webhooks: idempotência garantida pelo banco (unique constraint em `pagarme_order_id`)

---

## 12. Decisões Arquiteturais (ADRs)

### ADR-01 — Pagar.me via Abstração `IProcessadorPagamento`

**Contexto**: A plataforma precisa processar pagamentos PIX e transferências.

**Decisão**: Toda interação com a Pagar.me passa pela interface `IProcessadorPagamento`. Nenhum código de domínio ou aplicação importa a SDK da Pagar.me diretamente.

**Justificativa**: Provedores de pagamento mudam, têm indisponibilidades e podem ser trocados sem reescrita do domínio. A abstração também facilita mocks em testes. O custo é a criação de um adapter na camada Infrastructure.

**Consequência**: `Prontto.Infrastructure.Services.ProcessadorPagamentoPagarme` implementa `IProcessadorPagamento`. Futuramente, um segundo provedor pode ser adicionado sem alterar Application ou Domain.

---

### ADR-02 — Cloudinary via Abstração `IArmazenamentoArquivo`

**Contexto**: Uploads de foto de perfil, portfólio e imagens de chat.

**Decisão**: Toda operação de upload e deleção passa pela interface `IArmazenamentoArquivo`. Moderação de conteúdo é ativada no painel Cloudinary (sem código customizado para modelos de ML).

**Justificativa**: Mesma razão do ADR-01. Adicionalmente, delegar moderação ao Cloudinary evita complexidade de ML no backend.

**Consequência**: `Prontto.Infrastructure.Services.ArmazenamentoCloudinary` implementa `IArmazenamentoArquivo`.

---

### ADR-03 — Upload Direto (Browser → Cloudinary)

**Contexto**: Imagens pesadas passando pelo backend criam gargalo.

**Decisão**: Backend gera uma URL assinada (signed upload URL), browser faz upload direto ao Cloudinary. Backend recebe apenas a URL resultante.

**Justificativa**: Elimina o backend como intermediário de bytes. Reduz latência e custo de bandwidth. Padrão recomendado pelo próprio Cloudinary.

**Consequência**: Frontend precisa de dois passos: (1) requisitar URL assinada ao backend, (2) fazer upload direto. Mais complexidade no frontend, menos no backend.

---

### ADR-04 — Negociação via MensagemServico com TipoMensagem

**Contexto**: O fluxo de negociação precisa de propostas com valores rastreáveis.

**Decisão**: Propostas são `MensagemServico` com `TipoMensagem = Proposta` e campos `ValorProposta` / `StatusProposta`. Mensagens normais coexistem no mesmo stream.

**Justificativa**: Manter um único stream de comunicação simplifica o chat. Propostas visíveis no contexto da conversa facilitam a tomada de decisão. Alternativa de tabela separada adicionaria joins sem benefício claro.

**Consequência**: A máquina de estados de propostas deve ser gerenciada pelo `ServicoNegociacao`, que ao criar uma nova proposta marca a anterior como `Expirada`.

---

### ADR-05 — Auto-conclusão de 7 dias via Job (não via banco)

**Contexto**: Serviços precisam ser concluídos automaticamente se o cliente não responder em 7 dias.

**Decisão**: Job agendado (`IHostedService`) verifica periodicamente e aplica a transição.

**Justificativa**: Triggers de banco de dados criam lógica de negócio invisible, difícil de testar e monitorar. Jobs permitem logging, retry e observabilidade. A latência de ~1 hora é aceitável para um prazo de 7 dias.

**Consequência**: O campo `AguardandoConfirmacaoDesde` é obrigatório e indexado para a query do job ser eficiente.

---

### ADR-06 — OpenAPI 3.1 como Fonte da Verdade

**Contexto**: Frontend e backend precisam de contrato compartilhado.

**Decisão**: `api-spec/openapi.yaml` define o contrato. Backend implementa, frontend consome. Qualquer mudança de API começa no YAML.

**Justificativa**: Previne drift entre documentação e implementação. Permite geração de SDKs de cliente no futuro. Facilita onboarding.

**Consequência**: Em cada PR que altera endpoints, o `openapi.yaml` deve ser atualizado primeiro.

---

### ADR-07 — MySQL 8.0 com EF Core via Pomelo (sem Dapper)

**Contexto**: Necessidade de ORM para o backend C# em ambiente Hostinger (MySQL 8.0).

**Decisão**: EF Core 9 + `Pomelo.EntityFrameworkCore.MySql` com mapeamento explícito de colunas via `HasColumnName()`. Dapper apenas para queries analíticas complexas se necessário.

**Justificativa**: O ambiente de produção é Hostinger, que oferece MySQL 8.0. A migração de PostgreSQL para MySQL foi feita na sprint de infraestrutura. Pomelo é o provider mais maduro e ativamente mantido para MySQL + EF Core. Mapeamento explícito evita surpresas com convenções automáticas.

**Consequência**:
- Todas as entidades devem ter seus campos mapeados explicitamente em `ContextoBancoDados`
- Tipos específicos de PostgreSQL (`TIMESTAMPTZ`, `JSONB`, índices parciais) não existem no MySQL — usar `DATETIME`, `JSON` e indexes sem cláusula WHERE
- Connection string: `Server=srv2103.hstgr.io;Database=u6383509_PronttoAdm;User=u6383509_PronttoAdm;...`
- `SslMode=Required` obrigatório para conexões remotas ao Hostinger

---

### ADR-08 — Slugs com Sufixo Aleatório

**Contexto**: Perfis públicos precisam de URLs amigáveis únicas.

**Decisão**: Slug = `{nome-normalizado}-{4 chars hex aleatório}`. Exemplo: `joao-silva-a8f3`.

**Justificativa**: Nomes comuns colidem. Sufixo aleatório garante unicidade sem UUIDs feios na URL. 4 chars hex = 65.536 possibilidades por nome, suficiente para a escala esperada.

**Consequência**: Geração de slug deve verificar unicidade no banco antes de persistir. Retentativas em caso de colisão.

---

### ADR-09 — Slug de Prestador é Imutável após Publicação

**Contexto**: O slug compõe a URL pública do prestador (`/{cidadeSlug}/{categoriaSlug}/{slug}`). URLs são indexadas por motores de busca, compartilhadas por clientes e podem constar em materiais do prestador.

**Decisão**: Uma vez que o perfil do prestador é publicado (slug gerado), o slug **não pode ser alterado**, mesmo que o prestador altere seu nome cadastral.

**Justificativa**: Alterar o slug quebraria todos os links externos (SEO, bookmarks, QR codes, cartões de visita). A identidade da URL é separada da identidade do nome exibido. O `Nome` exibível pode ser atualizado; o `Slug` não.

**Consequência**:
- Campo `Slug` em `Usuario` é write-once: gravado na primeira publicação do perfil, nunca sobrescrito
- Endpoint de edição de perfil deve ignorar alterações no campo `slug` silenciosamente
- Frontend deve deixar claro ao prestador que o slug é permanente antes da publicação

---

### ADR-10 — Retenção Financeira via Controle da Aplicação (sem Escrow Nativo)

**Contexto**: O pagamento PIX precisa ser retido pela plataforma após confirmação, liberado somente após conclusão do serviço (ou reembolsado em cancelamento/disputa favorável ao cliente).

**Decisão**: A retenção é **controlada pela aplicação**, não por recurso de escrow nativo da Pagar.me.

**Fluxo financeiro**:
1. PIX é gerado e pago **antes** da execução do serviço
2. Após confirmação do webhook da Pagar.me: valor entra no saldo da plataforma (`Cobranca.Status = Retido`)
3. Após conclusão do serviço: `IProcessadorPagamento.TransferirAsync()` executa split 80/20 e transfere ao prestador (`Cobranca.Status = Liberado`)
4. Em disputa ativa: valor permanece `Retido` até decisão do Admin
5. Em cancelamento (se já pago): `IProcessadorPagamento.ReembolsarAsync()` devolve 100% ao cliente (`Cobranca.Status = Reembolsado`)

**Justificativa**: O recurso de escrow/split automático da Pagar.me vincula a liberação a eventos internos da plataforma de pagamento, reduzindo controle sobre o timing e a lógica de disputas. Controlar via aplicação mantém a lógica de negócio auditável, testável e independente do provedor.

**Consequência**:
- A plataforma precisa de uma conta Pagar.me com saldo disponível para fazer os repasses
- Risco de falha no repasse (coberto pelo Risco 1 e pelo Job de liberação com retry)
- Todo reembolso ou repasse grava `AuditLog` obrigatório
- Migração de provedor de pagamento não exige mudança na lógica de retenção (apenas no adapter)

---

## 13. Riscos Técnicos

### Risco 1 — Falha no Repasse ao Prestador

**Descrição**: A transferência PIX ao prestador falha após o serviço ser concluído e o pagamento já estar retido.

**Impacto**: Alto — prestador não recebe, confiança destruída.

**Mitigação**:
- Retry automático com backoff exponencial (3 tentativas)
- Alertas de monitoramento para cobranças `Retido` há mais de 24h após `Concluido`
- Fila de repasses pendentes para reprocessamento manual pelo admin
- Manter `LiberadoEm` nulo até confirmação da transferência

---

### Risco 2 — Webhook de Pagamento Duplicado ou Fora de Ordem

**Descrição**: Pagar.me pode reenviar webhooks. Processar duas vezes pode duplicar transições de estado.

**Impacto**: Alto — estado financeiro inconsistente.

**Mitigação**:
- Constraint `UNIQUE` em `charges.pagarme_order_id`
- Verificar status atual antes de aplicar transição (`if Status != Pendente, ignorar`)
- Armazenar `pagarme_payment_id` e verificar duplicidade na camada de webhook

---

### Risco 3 — Imagens Impróprias no Chat

**Descrição**: Usuário envia imagem com conteúdo impróprio antes da moderação automática processar.

**Impacto**: Médio — risco de reputação, possível infração legal.

**Mitigação**:
- Imagens não exibidas até `ImagemAprovada = true`
- Moderação síncrona se latência permitir (Cloudinary retorna resultado no upload com add-on)
- Fallback: moderação manual pelo admin para imagens com score limite

---

### Risco 4 — Refresh Token Comprometido (Roubo de Sessão)

**Descrição**: Um Refresh Token roubado (XSS, man-in-the-middle, vazamento de banco) permite ao atacante manter acesso por até 30 dias.

**Impacto**: Alto — acesso prolongado à conta do usuário, incluindo dados financeiros.

**Mitigação**:
- Cookie `HttpOnly; Secure; SameSite=Strict` impede acesso via JavaScript (mitiga XSS)
- Rotação obrigatória: uso do token roubado invalida a sessão legítima, alertando o usuário ao próximo acesso
- Hash SHA-256 no banco: vazamento do banco não expõe tokens brutos
- Admin pode revogar todas as sessões de um usuário via endpoint `POST /api/admin/users/{id}/revogar-sessoes`
- Monitorar múltiplos usos de token já revogado (sinal de comprometimento) — gerar `AuditLog` de alerta

---

### Risco 5 — Carga Inesperada no Chat

**Descrição**: Serviço de alto volume (ex: grande empresa usando a plataforma) gera volume de mensagens que sobrecarrega a solução de polling.

**Impacto**: Médio — degradação de performance, custo de banco.

**Mitigação V1**: Limitar chat a 100 mensagens por serviço via soft limit; paginação eficiente com índice composto.

**Mitigação V2**: Migrar para SignalR para eliminar polling. Escala horizontal do backend com sticky sessions (ou Redis backplane para SignalR stateless).

---

## 14. Próximos Passos

Sequência recomendada de implementação, do mais fundamental ao mais avançado:

### Sprint 1 — Fundação (já em andamento)

- [x] Estrutura do monorepo
- [x] Domain entities (`Usuario`, `Servico`, `Cobranca`, `DadosBancarios`, `MensagemServico`)
- [x] Enums base (`StatusServico`, `StatusCobranca`, `TipoConta`, `Papel`)
- [x] Auth (`ServicoAutenticacao`, JWT, BCrypt)
- [x] Controllers de Auth e Admin
- [x] Frontend: estrutura de features, AuthService, guards, interceptor
- [x] Docker Compose

### Sprint 2 — Domínio Expandido

- [ ] Adicionar entidades `Avaliacao`, `CategoriaUsuario`, `CidadeUsuario`, `ImagemPortfolio`
- [ ] Expandir `Usuario` com campos de perfil público (CPF, Slug, FotoPerfilUrl, etc.)
- [ ] Expandir `Servico` com `AguardandoConfirmacaoDesde`
- [ ] Expandir `Cobranca` com campos Pagar.me e PIX
- [ ] Expandir `MensagemServico` com `TipoMensagem`, `ValorProposta`, `StatusProposta`
- [ ] Novos enums: `TipoMensagem`, `StatusProposta`, `StatusCobranca` (expandido)
- [ ] Migration EF Core para novas tabelas e colunas
- [ ] Atualizar `openapi.yaml` com novos endpoints e schemas

### Sprint 3 — Serviços e Negociação

- [ ] `ServicoServico` (criar solicitação, vincular prestador, cancelar)
- [ ] `ServicoNegociacao` (enviar proposta, aceitar, contrapropor, regra de expiração)
- [ ] `IRepositorioServico` e `RepositorioServico` (com queries de busca)
- [ ] Controllers: `ControladorServicos`, `ControladorChat`
- [ ] Frontend: `ServicosComponent`, `MinhaAreaComponent` (listagem e chat)
- [ ] Testes unitários para `ServicoServico` e `ServicoNegociacao`

### Sprint 4 — Perfis Públicos e Descoberta

- [ ] `ServicoPerfilPrestador` (completar perfil, gerar slug, gerenciar categorias/cidades)
- [ ] `ServicoDescoberta` (busca pública por categoria + cidade, retornar prestadores ativos)
- [ ] Controller `ControladorPrestadores` (rota pública `GET /{cidadeSlug}/{categoriaSlug}/{slug}`)
- [ ] `IArmazenamentoArquivo` + `ArmazenamentoCloudinary` (upload assinado, deleção)
- [ ] Upload de foto de perfil e portfólio
- [ ] Frontend: `ParaPrestadoresComponent`, página pública do prestador
- [ ] Atualizar `openapi.yaml` com endpoints de descoberta

### Sprint 5 — Pagamentos

- [ ] `IProcessadorPagamento` (interface de domínio)
- [ ] `ProcessadorPagamentoPagarme` (adapter Infrastructure)
- [ ] `ServicoFinanceiro` (criar cobrança, gerar PIX, reter, liberar)
- [ ] Endpoint `POST /webhooks/pagarme` com validação HMAC
- [ ] `JobExpiracaoPix` (HangFire ou IHostedService)
- [ ] Testes de integração para fluxo de pagamento (Pagar.me sandbox)
- [ ] Frontend: tela de pagamento com QR Code PIX

### Sprint 6 — Conclusão, Avaliação e Jobs

- [ ] `ServicoAvaliacao` (registrar, validar janela de 30 dias)
- [ ] `JobConclusaoAutomatica` (7 dias de inatividade)
- [ ] `JobLiberacaoPagamento` (handler de `ServicoConcluido`)
- [ ] `JobMediaAvaliacao` (handler de `AvaliacaoRegistrada`)
- [ ] Frontend: `AvaliacaoComponent`, exibição de médias no perfil
- [ ] Testes unitários completos para todos os serviços

### Sprint 7 — Admin, Moderação e Polimento

- [ ] Expandir painel Admin (moderação de imagens, gestão de usuários, extrato financeiro)
- [ ] `JobLimpezaCloudinary`
- [ ] Rate limiting em todos os endpoints críticos
- [ ] Criptografia de CPF no banco
- [ ] Endpoint de exclusão de conta (LGPD)
- [ ] Testes de integração end-to-end

### Sprint 8 — Infraestrutura de Produção

- [ ] Variáveis de ambiente de produção documentadas
- [ ] CI/CD (GitHub Actions ou Azure DevOps)
- [ ] Migrations automáticas na inicialização (ou separadas, a definir)
- [ ] Observabilidade: structured logging (Serilog), health checks, métricas básicas
- [ ] HTTPS via NGINX
- [ ] Backups automáticos MySQL (Hostinger painel ou mysqldump via cron)

---

*Documento gerado em 2026-06-03. Última revisão: 2026-06-12. Manter atualizado a cada decisão arquitetural significativa.*
