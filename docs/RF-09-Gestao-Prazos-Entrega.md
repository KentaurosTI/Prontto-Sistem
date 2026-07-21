# RF-09 — Gestão de Prazos e Entrega do Sistema

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — classificação corrigida

---

## Objetivo

Documentar os requisitos contratuais e não-funcionais relacionados ao processo de entrega da plataforma Prontto ao cliente, incluindo prazos, protótipo, UAT e suporte pós-entrega.

---

## Classificação

> **ATENÇÃO**: RF-09 é um **requisito contratual/não-funcional de entrega de projeto**, não um requisito funcional do sistema Prontto.
>
> O `ARCHITECTURE.md` (v1.1) **não documenta nenhuma funcionalidade** de "gestão de prazos de entrega" como feature da plataforma. Não existe entidade, endpoint, caso de uso ou módulo no sistema para rastrear fases de prototipação, UAT ou suporte pós-entrega.
>
> Este documento preserva as informações do PDF original para fins de registro contratual, mas deixa claro que **nada aqui deve ser implementado como funcionalidade do software Prontto**.

---

## Descrição

O processo de entrega da plataforma segue um fluxo contratual com fases e prazos definidos entre a equipe de desenvolvimento e o cliente. As fases são: prototipação, aceite do protótipo, desenvolvimento, publicação em produção e período de suporte pós-entrega (UAT).

---

## Atores

| Ator | Descrição |
|------|-----------|
| Cliente (contratante do projeto) | Aprova protótipo, aceita entrega, solicita correções |
| Equipe de Desenvolvimento | Entrega protótipo, desenvolve e publica o sistema |

---

## Pré-condições (Contratuais)

- Contrato assinado entre as partes.
- Domínio e credenciais de hospedagem fornecidos pelo cliente.
- Protótipo apresentado e disponível para validação.

---

## Fluxos Principais (Contratuais)

### FP-01 — Prototipação e Aceite

1. Equipe entrega protótipo para validação.
2. Cliente analisa e aprova formalmente o protótipo.
3. **Prazo de desenvolvimento de 5 dias começa a contar a partir do aceite formal.**

### FP-02 — Desenvolvimento e Publicação

1. Equipe desenvolve o sistema conforme arquitetura e RFs aprovados.
2. Sistema é publicado no domínio do cliente em até **5 dias após aceite do protótipo**.
3. Deploy realizado conforme infraestrutura definida na arquitetura (Docker Compose, NGINX, PostgreSQL 17).

### FP-03 — Período de Suporte (UAT)

1. Após publicação, período de suporte pós-entrega de **7 a 15 dias** (a confirmar em contrato).
2. Equipe disponível para correção de bugs e ajustes durante o período de garantia.
3. Após o período: suporte adicional cobrado como consultoria.
4. Atrasos comunicados formalmente com justificativa (cláusula contratual).

---

## Fluxos de Exceção (Contratuais)

### FE-01 — Atraso na entrega

- Equipe comunica formalmente o atraso com justificativa escrita.
- Novo prazo negociado e documentado como aditivo contratual.

---

## Regras de Negócio (Contratuais)

| ID | Regra |
|----|-------|
| RN-01 | **Prazo de 5 dias** começa a contar a partir do aceite formal do protótipo pelo cliente. |
| RN-02 | Protótipo deve ser aprovado antes da subida em produção. |
| RN-03 | Período de suporte pós-entrega: **7 a 15 dias** (a confirmar em contrato). |
| RN-04 | Após o período de garantia: suporte adicional cobrado à parte como consultoria. |
| RN-05 | Atrasos comunicados formalmente por escrito com justificativa. |

---

## Eventos de Domínio

Nenhum. RF-09 não gera eventos de domínio no sistema Prontto.

---

## Entidades Envolvidas

Nenhuma entidade do sistema Prontto está envolvida neste RF.

---

## API Endpoints

Nenhum endpoint do sistema Prontto está associado a este RF.

---

## Critérios de Aceitação (Contratuais)

| ID | Critério |
|----|----------|
| CA-01 | Protótipo entregue para validação dentro do prazo acordado. |
| CA-02 | Sistema publicado em produção no domínio do cliente em até 5 dias após aceite formal do protótipo. |
| CA-03 | Período de suporte garantia ativo e equipe disponível para correção de bugs. |
| CA-04 | Comunicação formal documentada em caso de atraso. |

---

## Casos de Teste Funcionais

Não aplicável — RF-09 não possui funcionalidades testáveis no sistema.

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Funcionalidade de rastreamento de fases de entrega no sistema | RF-09 PDF | ARCHITECTURE.md não documenta nenhum módulo de gestão de projeto no software Prontto. |
| Painel de acompanhamento de UAT para o cliente | RF-09 PDF | Não documentado. Não é funcionalidade da plataforma marketplace. |
| Módulo de gestão de contratos | RF-09 PDF | Fora do escopo da plataforma Prontto. |

---

## Nota Arquitetural

Este RF deve ser tratado como **documentação contratual**, não como especificação de software. A infraestrutura de deploy descrita na arquitetura (Docker Compose, PostgreSQL 17, NGINX, HTTPS) é o que define como o sistema será publicado — não as ferramentas ou o ambiente de hospedagem mencionado no PDF original (Hostinger). Ver RF-10 para detalhes de infraestrutura e deploy.
