CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `categorias` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `nome` longtext CHARACTER SET utf8mb4 NOT NULL,
        `slug` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `ativo` tinyint(1) NOT NULL,
        `ordem_exibicao` int NOT NULL,
        CONSTRAINT `PK_categorias` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `cidades` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `nome` longtext CHARACTER SET utf8mb4 NOT NULL,
        `estado` longtext CHARACTER SET utf8mb4 NOT NULL,
        `slug` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `ativo` tinyint(1) NOT NULL,
        CONSTRAINT `PK_cidades` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `usuarios` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `nome` longtext CHARACTER SET utf8mb4 NOT NULL,
        `email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `telefone` longtext CHARACTER SET utf8mb4 NULL,
        `hash_senha` longtext CHARACTER SET utf8mb4 NOT NULL,
        `tipo_conta` longtext CHARACTER SET utf8mb4 NOT NULL,
        `papel` longtext CHARACTER SET utf8mb4 NOT NULL,
        `especialidade` longtext CHARACTER SET utf8mb4 NULL,
        `cidade_id` char(36) COLLATE ascii_general_ci NULL,
        `cpf` longtext CHARACTER SET utf8mb4 NULL,
        `url_foto_perfil` longtext CHARACTER SET utf8mb4 NULL,
        `slug` varchar(255) CHARACTER SET utf8mb4 NULL,
        `descricao` longtext CHARACTER SET utf8mb4 NULL,
        `media_avaliacoes` decimal(3,2) NOT NULL,
        `total_avaliacoes` int NOT NULL,
        `criado_em` datetime(6) NOT NULL,
        `atualizado_em` datetime(6) NOT NULL,
        `deletado_em` datetime(6) NULL,
        CONSTRAINT `PK_usuarios` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `dados_bancarios` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `tipo_chave_pix` longtext CHARACTER SET utf8mb4 NOT NULL,
        `chave_pix` longtext CHARACTER SET utf8mb4 NOT NULL,
        `nome_completo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `cpf_cnpj` longtext CHARACTER SET utf8mb4 NOT NULL,
        `nome_banco` longtext CHARACTER SET utf8mb4 NULL,
        `agencia` longtext CHARACTER SET utf8mb4 NULL,
        `numero_conta` longtext CHARACTER SET utf8mb4 NULL,
        `tipo_conta` longtext CHARACTER SET utf8mb4 NULL,
        `criado_em` datetime(6) NOT NULL,
        `atualizado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_dados_bancarios` PRIMARY KEY (`id`),
        CONSTRAINT `FK_dados_bancarios_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `imagens_portfolio` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `url` longtext CHARACTER SET utf8mb4 NOT NULL,
        `cloudinary_public_id` longtext CHARACTER SET utf8mb4 NOT NULL,
        `moderado` tinyint(1) NOT NULL,
        `aprovado` tinyint(1) NULL,
        `ordem_exibicao` int NOT NULL,
        `criado_em` datetime(6) NOT NULL,
        `deletado_em` datetime(6) NULL,
        CONSTRAINT `PK_imagens_portfolio` PRIMARY KEY (`id`),
        CONSTRAINT `FK_imagens_portfolio_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `logs_auditoria` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `usuario_id` char(36) COLLATE ascii_general_ci NULL,
        `acao` longtext CHARACTER SET utf8mb4 NOT NULL,
        `entidade` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `entidade_id` varchar(255) CHARACTER SET utf8mb4 NULL,
        `endereco_ip` longtext CHARACTER SET utf8mb4 NULL,
        `user_agent` longtext CHARACTER SET utf8mb4 NULL,
        `detalhes` longtext CHARACTER SET utf8mb4 NULL,
        `criado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_logs_auditoria` PRIMARY KEY (`id`),
        CONSTRAINT `FK_logs_auditoria_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `notificacoes` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `titulo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `mensagem` longtext CHARACTER SET utf8mb4 NOT NULL,
        `lido` tinyint(1) NOT NULL,
        `tipo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `referencia_id` longtext CHARACTER SET utf8mb4 NULL,
        `criado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_notificacoes` PRIMARY KEY (`id`),
        CONSTRAINT `FK_notificacoes_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `servicos` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `titulo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `descricao` longtext CHARACTER SET utf8mb4 NULL,
        `categoria_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `cidade_id` char(36) COLLATE ascii_general_ci NULL,
        `cliente_id` char(36) COLLATE ascii_general_ci NULL,
        `prestador_id` char(36) COLLATE ascii_general_ci NULL,
        `preco` decimal(10,2) NOT NULL,
        `taxa_admin_percentual` decimal(5,4) NOT NULL,
        `status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `endereco` longtext CHARACTER SET utf8mb4 NULL,
        `agendado_em` datetime(6) NULL,
        `concluido_em` datetime(6) NULL,
        `aguardando_confirmacao_desde` datetime(6) NULL,
        `criado_em` datetime(6) NOT NULL,
        `atualizado_em` datetime(6) NOT NULL,
        `deletado_em` datetime(6) NULL,
        CONSTRAINT `PK_servicos` PRIMARY KEY (`id`),
        CONSTRAINT `FK_servicos_categorias_categoria_id` FOREIGN KEY (`categoria_id`) REFERENCES `categorias` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_servicos_cidades_cidade_id` FOREIGN KEY (`cidade_id`) REFERENCES `cidades` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_servicos_usuarios_cliente_id` FOREIGN KEY (`cliente_id`) REFERENCES `usuarios` (`id`) ON DELETE SET NULL,
        CONSTRAINT `FK_servicos_usuarios_prestador_id` FOREIGN KEY (`prestador_id`) REFERENCES `usuarios` (`id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `tokens_renovacao` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `token` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `expira_em` datetime(6) NOT NULL,
        `revogado_em` datetime(6) NULL,
        `substituido_por` longtext CHARACTER SET utf8mb4 NULL,
        `endereco_ip` longtext CHARACTER SET utf8mb4 NULL,
        `user_agent` longtext CHARACTER SET utf8mb4 NULL,
        `criado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_tokens_renovacao` PRIMARY KEY (`id`),
        CONSTRAINT `FK_tokens_renovacao_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `usuarios_categorias` (
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `categoria_id` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_usuarios_categorias` PRIMARY KEY (`usuario_id`, `categoria_id`),
        CONSTRAINT `FK_usuarios_categorias_categorias_categoria_id` FOREIGN KEY (`categoria_id`) REFERENCES `categorias` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_usuarios_categorias_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `usuarios_cidades` (
        `usuario_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `cidade_id` char(36) COLLATE ascii_general_ci NOT NULL,
        CONSTRAINT `PK_usuarios_cidades` PRIMARY KEY (`usuario_id`, `cidade_id`),
        CONSTRAINT `FK_usuarios_cidades_cidades_cidade_id` FOREIGN KEY (`cidade_id`) REFERENCES `cidades` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_usuarios_cidades_usuarios_usuario_id` FOREIGN KEY (`usuario_id`) REFERENCES `usuarios` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `cobrancas` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `servico_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `valor_total` decimal(10,2) NOT NULL,
        `taxa_admin` decimal(10,2) NOT NULL,
        `valor_prestador` decimal(10,2) NOT NULL,
        `status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `pagarme_order_id` varchar(255) CHARACTER SET utf8mb4 NULL,
        `pagarme_payment_id` longtext CHARACTER SET utf8mb4 NULL,
        `pix_qr_code` longtext CHARACTER SET utf8mb4 NULL,
        `pix_copia_cola` longtext CHARACTER SET utf8mb4 NULL,
        `pix_expira_em` datetime(6) NULL,
        `pago_em` datetime(6) NULL,
        `retido_em` datetime(6) NULL,
        `liberado_em` datetime(6) NULL,
        `criado_em` datetime(6) NOT NULL,
        `atualizado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_cobrancas` PRIMARY KEY (`id`),
        CONSTRAINT `FK_cobrancas_servicos_servico_id` FOREIGN KEY (`servico_id`) REFERENCES `servicos` (`id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `disputas` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `servico_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `aberto_por_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `motivo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `descricao` longtext CHARACTER SET utf8mb4 NULL,
        `status` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `resolvido_por_id` char(36) COLLATE ascii_general_ci NULL,
        `decisao_admin` longtext CHARACTER SET utf8mb4 NULL,
        `criado_em` datetime(6) NOT NULL,
        `resolvido_em` datetime(6) NULL,
        CONSTRAINT `PK_disputas` PRIMARY KEY (`id`),
        CONSTRAINT `FK_disputas_servicos_servico_id` FOREIGN KEY (`servico_id`) REFERENCES `servicos` (`id`) ON DELETE CASCADE,
        CONSTRAINT `FK_disputas_usuarios_aberto_por_id` FOREIGN KEY (`aberto_por_id`) REFERENCES `usuarios` (`id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_disputas_usuarios_resolvido_por_id` FOREIGN KEY (`resolvido_por_id`) REFERENCES `usuarios` (`id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE TABLE `mensagens_servico` (
        `id` char(36) COLLATE ascii_general_ci NOT NULL,
        `servico_id` char(36) COLLATE ascii_general_ci NOT NULL,
        `remetente_id` char(36) COLLATE ascii_general_ci NULL,
        `papel_remetente` longtext CHARACTER SET utf8mb4 NOT NULL,
        `tipo_mensagem` longtext CHARACTER SET utf8mb4 NOT NULL,
        `conteudo` longtext CHARACTER SET utf8mb4 NOT NULL,
        `valor_proposta` decimal(10,2) NULL,
        `status_proposta` longtext CHARACTER SET utf8mb4 NULL,
        `imagem_moderada` tinyint(1) NOT NULL,
        `imagem_aprovada` tinyint(1) NULL,
        `criado_em` datetime(6) NOT NULL,
        CONSTRAINT `PK_mensagens_servico` PRIMARY KEY (`id`),
        CONSTRAINT `FK_mensagens_servico_servicos_servico_id` FOREIGN KEY (`servico_id`) REFERENCES `servicos` (`id`) ON DELETE CASCADE,
        CONSTRAINT `FK_mensagens_servico_usuarios_remetente_id` FOREIGN KEY (`remetente_id`) REFERENCES `usuarios` (`id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_categorias_slug` ON `categorias` (`slug`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_cidades_slug` ON `cidades` (`slug`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_cobrancas_pagarme_order_id` ON `cobrancas` (`pagarme_order_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_cobrancas_servico_id` ON `cobrancas` (`servico_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_cobrancas_status_pix_expira_em` ON `cobrancas` (`status`, `pix_expira_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_dados_bancarios_usuario_id` ON `dados_bancarios` (`usuario_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_disputas_aberto_por_id` ON `disputas` (`aberto_por_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_disputas_resolvido_por_id` ON `disputas` (`resolvido_por_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_disputas_servico_id` ON `disputas` (`servico_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_disputas_status` ON `disputas` (`status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_imagens_portfolio_usuario_id_ordem_exibicao` ON `imagens_portfolio` (`usuario_id`, `ordem_exibicao`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_logs_auditoria_criado_em` ON `logs_auditoria` (`criado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_logs_auditoria_entidade_entidade_id` ON `logs_auditoria` (`entidade`, `entidade_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_logs_auditoria_usuario_id_criado_em` ON `logs_auditoria` (`usuario_id`, `criado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_mensagens_servico_remetente_id` ON `mensagens_servico` (`remetente_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_mensagens_servico_servico_id_criado_em` ON `mensagens_servico` (`servico_id`, `criado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_notificacoes_usuario_id_lido_criado_em` ON `notificacoes` (`usuario_id`, `lido`, `criado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_aguardando_confirmacao_desde` ON `servicos` (`aguardando_confirmacao_desde`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_categoria_id` ON `servicos` (`categoria_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_cidade_id` ON `servicos` (`cidade_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_cliente_id` ON `servicos` (`cliente_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_prestador_id` ON `servicos` (`prestador_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_servicos_status` ON `servicos` (`status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_tokens_renovacao_token` ON `tokens_renovacao` (`token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_tokens_renovacao_usuario_id_revogado_em` ON `tokens_renovacao` (`usuario_id`, `revogado_em`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_usuarios_email` ON `usuarios` (`email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_usuarios_slug` ON `usuarios` (`slug`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_usuarios_categorias_categoria_id` ON `usuarios_categorias` (`categoria_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    CREATE INDEX `IX_usuarios_cidades_cidade_id` ON `usuarios_cidades` (`cidade_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260612185143_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260612185143_InitialCreate', '9.0.5');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

