-- ============================================================
--  Prontto — Database Schema
--  MySQL 8.0
--  Fonte da verdade: ARCHITECTURE.md v1.2
--  Gerado em: 2026-06-12
--
--  Produção (Hostinger):
--    Server  : srv2103.hstgr.io
--    Database: u6383509_PronttoAdm
--    User    : u6383509_PronttoAdm
--
--  Execução em ambiente limpo:
--    mysql -h srv2103.hstgr.io -u u6383509_PronttoAdm -p u6383509_PronttoAdm < schema.sql
--
--  Para recriar do zero (dev/CI apenas):
--    DROP DATABASE IF EXISTS prontto_dev;
--    CREATE DATABASE prontto_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--    USE prontto_dev;
--    source schema.sql;
--
--  Notas de compatibilidade MySQL vs PostgreSQL:
--  • ENUMs definidos inline por coluna (sem CREATE TYPE)
--  • TIMESTAMPTZ → DATETIME (UTC gerenciado pela aplicação)
--  • JSONB → JSON
--  • uuid_generate_v4() → UUID() (MySQL 8.0)
--  • Índices parciais (WHERE) não suportados — enforcement via lógica de aplicação
--  • ON UPDATE CURRENT_TIMESTAMP substitui triggers fn_set_updated_at
--  • CHAR(36) para PKs UUID; VARCHAR(255) para colunas com UNIQUE index
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
--  categorias
--  Catálogo canônico de categorias de serviço.
--  Toda referência a categoria usa FK aqui — nunca string livre.
-- ============================================================
CREATE TABLE categorias (
    id              CHAR(36)        NOT NULL DEFAULT (UUID()),
    nome            TEXT            NOT NULL,
    slug            VARCHAR(255)    NOT NULL,
    ativo           TINYINT(1)      NOT NULL DEFAULT 1,
    ordem_exibicao  INT             NOT NULL DEFAULT 0,

    CONSTRAINT pk_categorias        PRIMARY KEY (id),
    CONSTRAINT uq_categorias_slug   UNIQUE (slug),
    CONSTRAINT ck_categorias_display_order CHECK (ordem_exibicao >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Catálogo canônico de categorias. FK obrigatório — nunca string livre.';

-- ============================================================
--  cidades
--  Catálogo de cidades cobertas pela plataforma.
--  Toda referência a cidade usa FK aqui — nunca string livre.
-- ============================================================
CREATE TABLE cidades (
    id      CHAR(36)        NOT NULL DEFAULT (UUID()),
    nome    TEXT            NOT NULL,
    estado  CHAR(2)         NOT NULL,
    slug    VARCHAR(255)    NOT NULL,
    ativo   TINYINT(1)      NOT NULL DEFAULT 1,

    CONSTRAINT pk_cidades       PRIMARY KEY (id),
    CONSTRAINT uq_cidades_slug  UNIQUE (slug)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Cidades cobertas pela plataforma. FK obrigatório — nunca string livre.';

-- ============================================================
--  usuarios
--  Todos os usuários: clientes, prestadores e admins.
--  Soft delete via deletado_em.
-- ============================================================
CREATE TABLE usuarios (
    id                  CHAR(36)        NOT NULL DEFAULT (UUID()),
    nome                TEXT            NOT NULL,
    email               VARCHAR(255)    NOT NULL,
    telefone            TEXT,
    hash_senha          TEXT            NOT NULL,
    tipo_conta          ENUM('cliente','prestador')      NOT NULL,
    papel               ENUM('usuario','admin')          NOT NULL DEFAULT 'usuario',

    -- Campos de prestador
    especialidade       TEXT,
    cidade_id           CHAR(36),
    cpf                 TEXT,
    url_foto_perfil     TEXT,
    slug                VARCHAR(255),
    descricao           TEXT,

    -- Métricas calculadas
    media_avaliacoes    DECIMAL(3,2)    NOT NULL DEFAULT 0.00,
    total_avaliacoes    INT             NOT NULL DEFAULT 0,

    -- Auditoria de linha
    criado_em           DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deletado_em         DATETIME,

    CONSTRAINT pk_usuarios              PRIMARY KEY (id),
    CONSTRAINT uq_usuarios_email        UNIQUE (email),
    CONSTRAINT ck_usuarios_rating_average CHECK (media_avaliacoes BETWEEN 0 AND 5),
    CONSTRAINT ck_usuarios_rating_count   CHECK (total_avaliacoes >= 0),

    CONSTRAINT fk_usuarios_city FOREIGN KEY (cidade_id) REFERENCES cidades(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Usuários da plataforma: clientes, prestadores e admins.';

-- Índices de busca pública e login
CREATE INDEX ix_usuarios_account_type  ON usuarios(tipo_conta);
CREATE INDEX ix_usuarios_city_id       ON usuarios(cidade_id);
CREATE INDEX ix_usuarios_slug          ON usuarios(slug);
-- Nota: uniqueness de email para usuários ativos é garantida via lógica de aplicação (soft delete)

-- ============================================================
--  tokens_renovacao
--  Tokens de renovação de sessão JWT com rotação obrigatória.
--  Access Token: 15 min | Refresh Token: 30 dias
-- ============================================================
CREATE TABLE tokens_renovacao (
    id              CHAR(36)        NOT NULL DEFAULT (UUID()),
    usuario_id      CHAR(36)        NOT NULL,
    token           VARCHAR(64)     NOT NULL,   -- hash SHA-256 (64 chars hex)
    expira_em       DATETIME        NOT NULL,
    revogado_em     DATETIME,
    substituido_por VARCHAR(64),               -- hash do token sucessor
    endereco_ip     TEXT,
    user_agent      TEXT,
    criado_em       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_tokens_renovacao          PRIMARY KEY (id),
    CONSTRAINT uq_tokens_renovacao_token    UNIQUE (token),

    CONSTRAINT fk_tokens_renovacao_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tokens de renovação de sessão. Rotação obrigatória a cada uso. Hash SHA-256 armazenado.';

CREATE INDEX ix_tokens_renovacao_user_active    ON tokens_renovacao(usuario_id, expira_em);
CREATE INDEX ix_tokens_renovacao_expires_at     ON tokens_renovacao(expira_em);

-- ============================================================
--  usuarios_categorias
--  M2M: prestador ↔ categorias onde atua.
-- ============================================================
CREATE TABLE usuarios_categorias (
    usuario_id      CHAR(36)    NOT NULL,
    categoria_id    CHAR(36)    NOT NULL,

    CONSTRAINT pk_usuarios_categorias   PRIMARY KEY (usuario_id, categoria_id),
    CONSTRAINT fk_usuarios_categorias_user      FOREIGN KEY (usuario_id)   REFERENCES usuarios(id)   ON DELETE CASCADE,
    CONSTRAINT fk_usuarios_categorias_category  FOREIGN KEY (categoria_id) REFERENCES categorias(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Categorias de atuação do prestador. FK para categorias — nunca string livre.';

CREATE INDEX ix_usuarios_categorias_category_id ON usuarios_categorias(categoria_id);

-- ============================================================
--  usuarios_cidades
--  M2M: prestador ↔ cidades onde atua.
-- ============================================================
CREATE TABLE usuarios_cidades (
    usuario_id  CHAR(36)    NOT NULL,
    cidade_id   CHAR(36)    NOT NULL,

    CONSTRAINT pk_usuarios_cidades  PRIMARY KEY (usuario_id, cidade_id),
    CONSTRAINT fk_usuarios_cidades_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id)  ON DELETE CASCADE,
    CONSTRAINT fk_usuarios_cidades_city FOREIGN KEY (cidade_id)  REFERENCES cidades(id)   ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Cidades de atuação do prestador.';

CREATE INDEX ix_usuarios_cidades_city_id ON usuarios_cidades(cidade_id);

-- ============================================================
--  imagens_portfolio
--  Imagens de portfólio do prestador hospedadas no Cloudinary.
--  Soft delete via deletado_em.
-- ============================================================
CREATE TABLE imagens_portfolio (
    id                      CHAR(36)    NOT NULL DEFAULT (UUID()),
    usuario_id              CHAR(36)    NOT NULL,
    url                     TEXT        NOT NULL,
    cloudinary_public_id    TEXT        NOT NULL,
    moderado                TINYINT(1)  NOT NULL DEFAULT 0,
    aprovado                TINYINT(1),             -- NULL=pendente, 1=aprovada, 0=rejeitada
    ordem_exibicao          INT         NOT NULL DEFAULT 0,
    criado_em               DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deletado_em             DATETIME,

    CONSTRAINT pk_imagens_portfolio PRIMARY KEY (id),
    CONSTRAINT ck_imagens_portfolio_display_order CHECK (ordem_exibicao >= 0),
    CONSTRAINT fk_imagens_portfolio_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Imagens de trabalhos do prestador. Exibidas somente após aprovado = 1.';

CREATE INDEX ix_imagens_portfolio_user_active           ON imagens_portfolio(usuario_id, ordem_exibicao);
CREATE INDEX ix_imagens_portfolio_pending_moderation    ON imagens_portfolio(criado_em);

-- ============================================================
--  dados_bancarios
--  Dados PIX e bancários do prestador para recebimento.
--  Cardinalidade: 1 usuário — 1 registro.
-- ============================================================
CREATE TABLE dados_bancarios (
    id              CHAR(36)    NOT NULL DEFAULT (UUID()),
    usuario_id      CHAR(36)    NOT NULL,
    tipo_chave_pix  ENUM('cpf','cnpj','email','telefone','aleatoria') NOT NULL,
    chave_pix       TEXT        NOT NULL,
    nome_completo   TEXT        NOT NULL,
    cpf_cnpj        TEXT        NOT NULL,   -- AES-256 na aplicação (LGPD)
    nome_banco      TEXT,
    agencia         TEXT,
    numero_conta    TEXT,
    tipo_conta      TEXT,
    criado_em       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_dados_bancarios           PRIMARY KEY (id),
    CONSTRAINT uq_dados_bancarios_user_id   UNIQUE (usuario_id),
    CONSTRAINT fk_dados_bancarios_user      FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Dados bancários e PIX do prestador. Acesso restrito ao próprio prestador e admin (LGPD).';

-- ============================================================
--  servicos
--  Agregado central da plataforma.
--  Representa a ordem de serviço do início ao fim.
--  Soft delete via deletado_em.
-- ============================================================
CREATE TABLE servicos (
    id                              CHAR(36)        NOT NULL DEFAULT (UUID()),
    titulo                          TEXT            NOT NULL,
    descricao                       TEXT,
    categoria_id                    CHAR(36)        NOT NULL,
    cidade_id                       CHAR(36),
    cliente_id                      CHAR(36),
    prestador_id                    CHAR(36),
    preco                           DECIMAL(12,2)   NOT NULL DEFAULT 0,
    taxa_admin_percentual           DECIMAL(6,4)    NOT NULL DEFAULT 0.2000,
    status                          ENUM(
                                        'em_negociacao',
                                        'aguardando_pagamento',
                                        'pago',
                                        'em_andamento',
                                        'aguardando_confirmacao_cliente',
                                        'em_disputa',
                                        'concluido',
                                        'cancelado'
                                    ) NOT NULL DEFAULT 'em_negociacao',
    endereco                        TEXT,
    agendado_em                     DATETIME,
    concluido_em                    DATETIME,
    aguardando_confirmacao_desde    DATETIME,
    criado_em                       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em                   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deletado_em                     DATETIME,

    CONSTRAINT pk_servicos              PRIMARY KEY (id),
    CONSTRAINT ck_servicos_price        CHECK (preco >= 0),
    CONSTRAINT ck_servicos_admin_fee_rate CHECK (taxa_admin_percentual BETWEEN 0 AND 1),

    CONSTRAINT fk_servicos_category FOREIGN KEY (categoria_id) REFERENCES categorias(id) ON DELETE RESTRICT,
    CONSTRAINT fk_servicos_city     FOREIGN KEY (cidade_id)    REFERENCES cidades(id)    ON DELETE SET NULL,
    CONSTRAINT fk_servicos_client   FOREIGN KEY (cliente_id)   REFERENCES usuarios(id)   ON DELETE SET NULL,
    CONSTRAINT fk_servicos_provider FOREIGN KEY (prestador_id) REFERENCES usuarios(id)   ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Agregado central da plataforma. Ciclo completo: negociação → pagamento → execução → conclusão.';

CREATE INDEX ix_servicos_client_id              ON servicos(cliente_id, criado_em);
CREATE INDEX ix_servicos_provider_id            ON servicos(prestador_id, criado_em);
CREATE INDEX ix_servicos_status                 ON servicos(status, criado_em);
CREATE INDEX ix_servicos_category_id            ON servicos(categoria_id);
CREATE INDEX ix_servicos_city_id                ON servicos(cidade_id);
-- CRÍTICO: job de auto-conclusão em 7 dias (JobConclusaoAutomatica)
CREATE INDEX ix_servicos_awaiting_confirmation  ON servicos(aguardando_confirmacao_desde);

-- ============================================================
--  cobrancas
--  Ciclo financeiro de um serviço.
--  1 serviço → 1 cobrança (unique servico_id).
-- ============================================================
CREATE TABLE cobrancas (
    id                  CHAR(36)        NOT NULL DEFAULT (UUID()),
    servico_id          CHAR(36)        NOT NULL,
    valor_total         DECIMAL(12,2)   NOT NULL,
    taxa_admin          DECIMAL(12,2)   NOT NULL,
    valor_prestador     DECIMAL(12,2)   NOT NULL,
    status              ENUM('pendente','pago','retido','liberado','reembolsado','cancelado') NOT NULL DEFAULT 'pendente',
    pagarme_order_id    VARCHAR(255),   -- VARCHAR para suportar UNIQUE index
    pagarme_payment_id  VARCHAR(255),
    pix_qr_code         LONGTEXT,
    pix_copia_cola      TEXT,
    pix_expira_em       DATETIME,
    pago_em             DATETIME,
    retido_em           DATETIME,
    liberado_em         DATETIME,
    criado_em           DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT pk_cobrancas                     PRIMARY KEY (id),
    CONSTRAINT uq_cobrancas_service_id          UNIQUE (servico_id),
    -- Idempotência de webhook: pagarme_order_id UNIQUE garante que webhooks duplicados não reprocessam
    CONSTRAINT uq_cobrancas_pagarme_order_id    UNIQUE (pagarme_order_id),
    CONSTRAINT ck_cobrancas_total_amount        CHECK (valor_total > 0),
    CONSTRAINT ck_cobrancas_admin_fee           CHECK (taxa_admin >= 0),
    CONSTRAINT ck_cobrancas_provider_amount     CHECK (valor_prestador >= 0),

    CONSTRAINT fk_cobrancas_service FOREIGN KEY (servico_id) REFERENCES servicos(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Transação financeira de um serviço. Criada quando serviço avança para aguardando_pagamento.';

-- CRÍTICO: job de expiração de PIX (JobExpiracaoPix)
CREATE INDEX ix_cobrancas_pix_expiry    ON cobrancas(pix_expira_em);
CREATE INDEX ix_cobrancas_status        ON cobrancas(status, criado_em);
CREATE INDEX ix_cobrancas_retidas       ON cobrancas(retido_em);

-- ============================================================
--  mensagens_servico
--  Chat do serviço. Suporta texto, imagens e propostas.
-- ============================================================
CREATE TABLE mensagens_servico (
    id              CHAR(36)    NOT NULL DEFAULT (UUID()),
    servico_id      CHAR(36)    NOT NULL,
    remetente_id    CHAR(36),               -- NULL para mensagens de sistema
    papel_remetente ENUM('cliente','prestador','admin','sistema') NOT NULL,
    tipo_mensagem   ENUM('texto','imagem','proposta','sistema') NOT NULL,
    conteudo        TEXT        NOT NULL,
    valor_proposta  DECIMAL(12,2),
    status_proposta ENUM('pendente','aceita','recusada','expirada'),
    imagem_moderada TINYINT(1)  NOT NULL DEFAULT 0,
    imagem_aprovada TINYINT(1),
    criado_em       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_mensagens_servico PRIMARY KEY (id),
    CONSTRAINT fk_mensagens_servico_service FOREIGN KEY (servico_id)   REFERENCES servicos(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_mensagens_servico_sender  FOREIGN KEY (remetente_id) REFERENCES usuarios(id)  ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Chat do serviço. Texto, imagens e propostas num único stream.';

-- PRINCIPAL: paginação cursor-based do chat
CREATE INDEX ix_mensagens_servico_chat_cursor ON mensagens_servico(servico_id, criado_em, id);
-- Nota: unique index parcial (somente 1 proposta pendente por serviço) não é suportado no MySQL.
--       A constraint é garantida pelo ServicoNegociacao na camada de aplicação.

-- ============================================================
--  avaliacoes
--  Avaliação bilateral após conclusão.
-- ============================================================
CREATE TABLE avaliacoes (
    id           CHAR(36)    NOT NULL DEFAULT (UUID()),
    servico_id   CHAR(36)    NOT NULL,
    avaliador_id CHAR(36)    NOT NULL,
    avaliado_id  CHAR(36)    NOT NULL,
    nota         SMALLINT    NOT NULL,
    comentario   TEXT,
    criado_em    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_avaliacoes                    PRIMARY KEY (id),
    CONSTRAINT uq_avaliacoes_service_reviewer   UNIQUE (servico_id, avaliador_id),
    CONSTRAINT ck_avaliacoes_rating             CHECK (nota BETWEEN 1 AND 5),
    CONSTRAINT ck_avaliacoes_self_review        CHECK (avaliador_id != avaliado_id),

    CONSTRAINT fk_avaliacoes_service  FOREIGN KEY (servico_id)   REFERENCES servicos(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_avaliacoes_reviewer FOREIGN KEY (avaliador_id) REFERENCES usuarios(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_avaliacoes_reviewed FOREIGN KEY (avaliado_id)  REFERENCES usuarios(id)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Avaliação bilateral pós-conclusão. Unique(servico_id, avaliador_id) impede avaliação dupla.';

CREATE INDEX ix_avaliacoes_reviewed_id  ON avaliacoes(avaliado_id, criado_em);
CREATE INDEX ix_avaliacoes_service_id   ON avaliacoes(servico_id);

-- ============================================================
--  disputas
--  Contestação de conclusão aberta pelo cliente.
--  1 disputa por serviço (unique servico_id).
-- ============================================================
CREATE TABLE disputas (
    id               CHAR(36)    NOT NULL DEFAULT (UUID()),
    servico_id       CHAR(36)    NOT NULL,
    aberto_por_id    CHAR(36)    NOT NULL,
    motivo           TEXT        NOT NULL,
    descricao        TEXT,
    status           ENUM('aberta','em_analise','resolvida_cliente','resolvida_prestador') NOT NULL DEFAULT 'aberta',
    resolvido_por_id CHAR(36),
    decisao_admin    TEXT,
    criado_em        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolvido_em     DATETIME,

    CONSTRAINT pk_disputas              PRIMARY KEY (id),
    CONSTRAINT uq_disputas_service_id   UNIQUE (servico_id),

    CONSTRAINT fk_disputas_service      FOREIGN KEY (servico_id)       REFERENCES servicos(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_disputas_opened_by    FOREIGN KEY (aberto_por_id)    REFERENCES usuarios(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_disputas_resolved_by  FOREIGN KEY (resolvido_por_id) REFERENCES usuarios(id)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Contestação de conclusão. Criada pelo cliente de AguardandoConfirmacaoCliente. Pagamento permanece retido até resolução.';

CREATE INDEX ix_disputas_open          ON disputas(criado_em);
CREATE INDEX ix_disputas_opened_by_id  ON disputas(aberto_por_id);

-- ============================================================
--  notificacoes
--  Notificações in-app geradas por eventos de domínio.
-- ============================================================
CREATE TABLE notificacoes (
    id            CHAR(36)    NOT NULL DEFAULT (UUID()),
    usuario_id    CHAR(36)    NOT NULL,
    titulo        TEXT        NOT NULL,
    mensagem      TEXT        NOT NULL,
    lido          TINYINT(1)  NOT NULL DEFAULT 0,
    tipo          ENUM('proposta','pagamento','disputa','avaliacao','conclusao','sistema') NOT NULL,
    referencia_id TEXT,
    criado_em     DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_notificacoes  PRIMARY KEY (id),
    CONSTRAINT fk_notificacoes_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Notificações in-app. Geradas por handlers de eventos de domínio (V1: polling 10s; V2: SignalR).';

-- PRINCIPAL: não-lidas por usuário (polling a cada 10s no frontend)
CREATE INDEX ix_notificacoes_user_unread    ON notificacoes(usuario_id, lido, criado_em);
CREATE INDEX ix_notificacoes_user_all       ON notificacoes(usuario_id, criado_em);

-- ============================================================
--  logs_auditoria
--  Trilha de auditoria imutável (append-only).
--  NUNCA deletar registros. Sem atualizado_em.
-- ============================================================
CREATE TABLE logs_auditoria (
    id          CHAR(36)    NOT NULL DEFAULT (UUID()),
    usuario_id  CHAR(36),               -- NULL para ações de job/sistema
    acao        TEXT        NOT NULL,
    entidade    TEXT        NOT NULL,
    entidade_id TEXT,
    endereco_ip TEXT,
    user_agent  TEXT,
    detalhes    JSON,                   -- contexto adicional (campos alterados, motivo, etc.)
    criado_em   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pk_logs_auditoria    PRIMARY KEY (id),
    CONSTRAINT fk_logs_auditoria_user FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Trilha de auditoria IMUTÁVEL. Registros NUNCA devem ser deletados. Tabela append-only.';

CREATE INDEX ix_logs_auditoria_user_created ON logs_auditoria(usuario_id, criado_em);
-- Prefixo de 100 chars para colunas TEXT em indexes do MySQL
CREATE INDEX ix_logs_auditoria_entity       ON logs_auditoria(entidade(100), entidade_id(100), criado_em);
CREATE INDEX ix_logs_auditoria_created_at   ON logs_auditoria(criado_em);
CREATE INDEX ix_logs_auditoria_action       ON logs_auditoria(acao(100), criado_em);

-- ============================================================
--  VIEWS — Queries frequentes pré-compiladas
-- ============================================================

-- Prestadores ativos com suas cidades (busca pública)
CREATE OR REPLACE VIEW vw_prestadores_publicos AS
SELECT
    u.id,
    u.nome,
    u.slug,
    u.descricao,
    u.url_foto_perfil,
    u.media_avaliacoes,
    u.total_avaliacoes,
    ci.id       AS cidade_id,
    ci.slug     AS city_slug,
    ci.nome     AS city_name,
    ci.estado   AS city_state,
    cat.id      AS categoria_id,
    cat.slug    AS category_slug,
    cat.nome    AS category_name
FROM usuarios u
JOIN usuarios_cidades    uc  ON uc.usuario_id   = u.id
JOIN cidades             ci  ON ci.id           = uc.cidade_id
JOIN usuarios_categorias uca ON uca.usuario_id  = u.id
JOIN categorias          cat ON cat.id          = uca.categoria_id
WHERE u.tipo_conta  = 'prestador'
  AND u.deletado_em IS NULL
  AND ci.ativo      = 1
  AND cat.ativo     = 1;

-- Serviços que atingiram 7 dias sem resposta (JobConclusaoAutomatica)
CREATE OR REPLACE VIEW vw_servicos_para_autoconclusao AS
SELECT
    s.id            AS servico_id,
    s.prestador_id,
    s.cliente_id,
    s.aguardando_confirmacao_desde,
    s.preco,
    s.taxa_admin_percentual,
    c.id            AS charge_id,
    c.valor_prestador
FROM servicos s
JOIN cobrancas c ON c.servico_id = s.id
WHERE s.status = 'aguardando_confirmacao_cliente'
  AND s.aguardando_confirmacao_desde IS NOT NULL
  AND s.aguardando_confirmacao_desde < DATE_SUB(NOW(), INTERVAL 7 DAY)
  AND s.deletado_em IS NULL
  AND c.status = 'retido';

-- PIX vencidos (JobExpiracaoPix)
CREATE OR REPLACE VIEW vw_cobrancas_pix_expirado AS
SELECT
    c.id            AS charge_id,
    c.servico_id,
    c.pix_expira_em,
    c.pagarme_order_id
FROM cobrancas c
WHERE c.status        = 'pendente'
  AND c.pix_expira_em IS NOT NULL
  AND c.pix_expira_em < NOW();

-- Cobranças retidas sem liberação há > 24h (alerta de risco)
CREATE OR REPLACE VIEW vw_cobrancas_retidas_alerta AS
SELECT
    c.id            AS charge_id,
    c.servico_id,
    c.valor_prestador,
    c.retido_em,
    s.prestador_id
FROM cobrancas c
JOIN servicos s ON s.id = c.servico_id
WHERE c.status   = 'retido'
  AND s.status   = 'concluido'
  AND c.retido_em < DATE_SUB(NOW(), INTERVAL 24 HOUR);

-- Refresh tokens ativos por usuário (admin: ver sessões ativas)
CREATE OR REPLACE VIEW vw_sessoes_ativas AS
SELECT
    rt.id,
    rt.usuario_id,
    u.nome      AS user_name,
    u.email,
    rt.endereco_ip,
    rt.user_agent,
    rt.criado_em,
    rt.expira_em
FROM tokens_renovacao rt
JOIN usuarios u ON u.id = rt.usuario_id
WHERE rt.revogado_em IS NULL
  AND rt.expira_em   > NOW();

-- ============================================================
--  SEED DATA — Categorias e Cidades iniciais
-- ============================================================

INSERT INTO categorias (id, nome, slug, ativo, ordem_exibicao) VALUES
    (UUID(), 'Encanador',           'encanador',        1,  1),
    (UUID(), 'Eletricista',         'eletricista',       1,  2),
    (UUID(), 'Pintor',              'pintor',            1,  3),
    (UUID(), 'Pedreiro',            'pedreiro',          1,  4),
    (UUID(), 'Marceneiro',          'marceneiro',        1,  5),
    (UUID(), 'Diarista',            'diarista',          1,  6),
    (UUID(), 'Jardineiro',          'jardineiro',        1,  7),
    (UUID(), 'Técnico de Ar Cond.', 'ar-condicionado',   1,  8),
    (UUID(), 'Serralheiro',         'serralheiro',       1,  9),
    (UUID(), 'Dedetizador',         'dedetizador',       1, 10);

INSERT INTO cidades (id, nome, estado, slug, ativo) VALUES
    (UUID(), 'Itapevi',        'SP', 'itapevi',        1),
    (UUID(), 'São Paulo',      'SP', 'sao-paulo',      1),
    (UUID(), 'Osasco',         'SP', 'osasco',         1),
    (UUID(), 'Carapicuíba',    'SP', 'carapicuiba',    1),
    (UUID(), 'Cotia',          'SP', 'cotia',          1),
    (UUID(), 'Barueri',        'SP', 'barueri',        1),
    (UUID(), 'Rio de Janeiro', 'RJ', 'rio-de-janeiro', 1),
    (UUID(), 'Belo Horizonte', 'MG', 'belo-horizonte', 1),
    (UUID(), 'Curitiba',       'PR', 'curitiba',       1),
    (UUID(), 'Porto Alegre',   'RS', 'porto-alegre',   1);

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
--  NOTAS DE PRODUÇÃO
-- ============================================================

-- PAGINAÇÃO
-- • Listagens (serviços, usuários, cobranças): OFFSET/LIMIT com max 50/página
--   SELECT ... ORDER BY criado_em DESC, id DESC LIMIT $limit OFFSET $offset
-- • Chat (mensagens_servico): cursor-based para evitar drift
--   WHERE criado_em < $cursor_ts OR (criado_em = $cursor_ts AND id < $cursor_id)
--   ORDER BY criado_em DESC, id DESC LIMIT 50
-- • Audit logs: cursor-based (tabela de alto volume)

-- SOFT DELETE
-- • Tabelas: usuarios, servicos, imagens_portfolio
-- • Filtro automático via EF Core Global Query Filter (deletado_em IS NULL)
-- • MySQL não tem RLS nativo — todo acesso passa pelo filtro do EF Core

-- ÍNDICES PARCIAIS (DIFERENÇA vs PostgreSQL)
-- • MySQL NÃO suporta índices parciais (WHERE condition)
-- • Constraints antes garantidas por WHERE foram removidas dos índices
-- • Enforcement via lógica de aplicação:
--   - Email único para usuários ativos: verificar deletado_em IS NULL no ServicoAutenticacao
--   - Slug único para usuários ativos: verificar deletado_em IS NULL ao gerar slug
--   - Máximo 1 proposta pendente por serviço: ServicoNegociacao marca anterior como Expirada

-- AUDITORIA
-- • logs_auditoria é append-only. Nunca executar DELETE ou UPDATE nesta tabela.
-- • Arquivamento LGPD: após 90 dias, logs podem ser movidos para tabela de arquivo frio.

-- PERFORMANCE (MySQL 8.0)
-- • innodb_buffer_pool_size = 25% da RAM disponível
-- • max_connections = 100 (configurar pool no EF Core: Max Pool Size=100)
-- • slow_query_log habilitado em produção (threshold: 2s)
-- • ANALYZE TABLE mensagens_servico, logs_auditoria periodicamente

-- BACKUPS (Hostinger)
-- • Usar painel Hostinger para backups automáticos diários
-- • Ou mysqldump via cron: mysqldump -h srv2103.hstgr.io -u u6383509_PronttoAdm -p u6383509_PronttoAdm > backup_$(date +%Y%m%d).sql

-- FIM DO SCHEMA
