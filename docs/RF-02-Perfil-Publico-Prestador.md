# RF-02 — Perfil Público do Prestador

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Disponibilizar uma página pública para cada Prestador cadastrado, acessível sem autenticação, exibindo informações de perfil, portfólio de imagens (após moderação), avaliações recebidas e ação de contratação.

---

## Descrição

Cada Prestador possui uma URL pública no formato `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`. O perfil é criado ao Prestador completar seu cadastro (caso de uso PR-02). O **Slug é imutável após publicação** (ADR-09): a URL pública é permanente, mesmo que o Prestador altere seu nome exibido.

Imagens do portfólio passam por moderação automática via Cloudinary antes de serem exibidas publicamente. O botão de contratação redireciona ao fluxo de criação de solicitação de serviço (RF-04), que exige login do Cliente.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Visitante | Qualquer usuário, autenticado ou não, que acessa o perfil público |
| Prestador | Usuário com `TipoConta = Prestador` que gerencia seu próprio perfil |
| Cliente | Usuário com `TipoConta = Cliente` que pode iniciar uma solicitação a partir do perfil |
| Admin | Pode moderar imagens do portfólio (AD-08) |
| Sistema (Cloudinary) | Modera automaticamente imagens enviadas ao portfólio |

---

## Pré-condições

- O Prestador deve ter completado o perfil (caso de uso PR-02): `Slug` gerado, pelo menos uma `Categoria` e uma `Cidade` associadas.
- Para enviar imagens ao portfólio: Prestador autenticado.
- Para abrir disputa ou contratar: Cliente autenticado.

---

## Fluxos Principais

### FP-01 — Visualização do Perfil Público

1. Visitante acessa `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`.
2. Sistema localiza o Prestador pelo `Slug` (query por `Usuario.Slug` com filtro `TipoConta = Prestador`).
3. Sistema retorna dados públicos:
   - Nome, foto de perfil (`FotoPerfilUrl`), descrição (`Descricao`)
   - Especialidade, cidade(s) de atuação, categoria(s)
   - Média de avaliações (`MediaAvaliacoes`) e total (`TotalAvaliacoes`)
   - Imagens do portfólio com `Aprovada = true` e `DeletadoEm IS NULL`, ordenadas por `Ordem`
   - Avaliações recebidas (lista paginada — `AvaliadoId = Prestador.Id`)
4. Frontend renderiza a página pública.
5. Botão "Contratar": se visitante não autenticado → redireciona para `/entrar`; se Cliente autenticado → abre fluxo de solicitação (RF-04).

### FP-02 — Completar Perfil do Prestador (PR-02)

1. Prestador autenticado acessa área de edição de perfil.
2. Prestador preenche: foto de perfil, CPF, descrição, categorias de atuação (1 ou mais `CategoriaId`), cidades de cobertura (1 ou mais `CidadeId`), dados bancários (via PR-04).
3. Sistema valida os dados via `FluentValidation`.
4. Se `Slug` ainda não gerado:
   - Sistema gera `Slug` no padrão `{nome-normalizado}-{4-chars-hex}` (ADR-08).
   - Sistema verifica unicidade no banco; retenta em caso de colisão.
   - `Slug` é **gravado uma única vez** — imutável após este ponto (ADR-09).
5. Sistema persiste `CategoriaUsuario` (tabela `usuarios_categorias`) e `CidadeUsuario` (tabela `usuarios_cidades`).
6. Sistema publica evento `PerfilPrestadorCompleto`.
7. Perfil fica disponível na URL pública.

### FP-03 — Adicionar Imagens ao Portfólio (PR-03)

1. Prestador autenticado acessa gerenciamento de portfólio.
2. Sistema gera URL assinada via `IArmazenamentoArquivo.GerarUrlAssinadaAsync()` (ADR-03).
3. Frontend faz upload direto ao Cloudinary usando a URL assinada (backend não recebe bytes).
4. Após upload, frontend envia a URL resultante ao backend.
5. Sistema cria registro `ImagemPortfolio` com `Moderada = false`, `Aprovada = null`.
6. Sistema publica evento `ImagemSubmetidaModeracao` (payload: `CloudinaryPublicId`, `TipoReferencia = portfolio`).
7. Cloudinary processa a moderação automaticamente (Add-on de moderação habilitado).
8. Webhook/callback de resultado chama `ServicoModeracao.ProcessarResultadoAsync()`:
   - Aprovada: `Moderada = true`, `Aprovada = true` — imagem exibida no perfil.
   - Rejeitada: `Moderada = true`, `Aprovada = false` — imagem não exibida; arquivo removido do Cloudinary (Job SY-06).
9. Sistema publica `ImagemAprovada` ou `ImagemRejeitada`.

### FP-04 — Editar Informações do Perfil

1. Prestador autenticado acessa edição do perfil.
2. Prestador pode alterar: nome exibido, foto de perfil, descrição, categorias, cidades.
3. Sistema persiste as alterações.
4. **O campo `Slug` é ignorado silenciosamente** mesmo que enviado no payload — nunca sobrescrito (ADR-09).
5. Sistema grava `AuditLog` com `Acao = "usuario.perfil_alterado"`.

---

## Fluxos Alternativos

### FA-01 — Perfil não encontrado

- **Ponto de desvio**: Passo 2 do FP-01 — `Slug` não encontrado no banco.
- **Ação**: Sistema retorna `404 Not Found`.
- **Continuação**: Frontend exibe página de não encontrado.

### FA-02 — Prestador sem imagens aprovadas

- **Ponto de desvio**: Passo 3 do FP-01 — nenhuma `ImagemPortfolio` com `Aprovada = true`.
- **Ação**: Galeria de portfólio exibida vazia (sem erro).
- **Continuação**: Frontend exibe mensagem "Nenhuma imagem no portfólio ainda".

### FA-03 — Imagem rejeitada na moderação

- **Ponto de desvio**: Passo 8 do FP-03 — Cloudinary rejeita a imagem.
- **Ação**: `Aprovada = false`, imagem não exibida; Job SY-06 remove o arquivo do Cloudinary.
- **Continuação**: Prestador pode submeter nova imagem.

### FA-04 — Visitante não autenticado clica em "Contratar"

- **Ponto de desvio**: Passo 5 do FP-01 — visitante sem autenticação.
- **Ação**: Frontend redireciona para `/entrar` com parâmetro de retorno ao perfil.
- **Continuação**: Após login, Cliente é redirecionado de volta ao perfil e pode iniciar a solicitação.

---

## Fluxos de Exceção

### FE-01 — Arquivo muito grande no upload

- **Ponto de desvio**: Passo 3 do FP-03 — arquivo excede 10 MB.
- **Ação**: Upload rejeitado pelo Cloudinary. Frontend exibe erro.
- **Continuação**: Prestador submete imagem menor.

### FE-02 — Tipo de arquivo inválido

- **Ponto de desvio**: Passo 3 do FP-03 — tipo diferente de `image/jpeg`, `image/png`, `image/webp`.
- **Ação**: Requisição rejeitada pela validação do backend antes de gerar URL assinada.
- **Continuação**: Frontend exibe erro de tipo não suportado.

---

## Pós-condições

- Perfil público acessível em `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`.
- Imagens do portfólio moderadas e aprovadas exibidas publicamente.
- Evento `PerfilPrestadorCompleto` publicado ao completar perfil pela primeira vez.
- `Slug` gerado e imutável.
- `AuditLog` gravado para alterações de perfil.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | O `Slug` do Prestador é gerado no formato `{nome-normalizado}-{4-chars-hex}` e é **imutável após publicação** (ADR-09). Endpoint de edição de perfil ignora silenciosamente qualquer alteração no campo `slug`. |
| RN-02 | Imagens do portfólio só são exibidas publicamente após `Aprovada = true`. Imagens com `Aprovada = null` (pendentes) não são exibidas. |
| RN-03 | Upload de imagem segue o padrão de URL assinada (ADR-03): backend gera a URL assinada, browser faz upload direto ao Cloudinary. Backend nunca recebe bytes do arquivo. |
| RN-04 | Tamanho máximo de imagem: **10 MB**. Tipos aceitos: `image/jpeg`, `image/png`, `image/webp`. |
| RN-05 | Categorias e cidades são referenciadas por FK (`CategoriaId`, `CidadeId`) nas tabelas `usuarios_categorias` e `usuarios_cidades` — nunca por string livre. |
| RN-06 | O perfil público exibe exclusivamente: Nome, Foto, Especialidade, Cidade(s), Nota média e portfólio aprovado. CPF, e-mail e dados bancários **nunca são exibidos publicamente** (LGPD). |
| RN-07 | A página de perfil público `/{cidadeSlug}/{categoriaSlug}/{slug}` é acessível **sem autenticação**. |
| RN-08 | Imagens rejeitadas pela moderação têm o arquivo removido do Cloudinary pelo Job SY-06. |
| RN-09 | O prestador pode alterar nome exibido, foto, descrição, categorias e cidades a qualquer momento. A URL pública (slug) não é afetada. |
| RN-10 | Avaliações recebidas (AvaliadoId = PrestadorId) ficam visíveis publicamente no perfil, com nota, comentário e data. |

---

## Eventos de Domínio

| Evento | Publicado quando | Payload |
|--------|-----------------|---------|
| `PerfilPrestadorCompleto` | Slug gerado ao completar perfil | `{ PrestadorId, Slug, CategoriaSlug, CidadeSlug }` |
| `ImagemSubmetidaModeracao` | Imagem enviada ao Cloudinary | `{ Referencia, TipoReferencia: "portfolio", CloudinaryPublicId }` |
| `ImagemAprovada` | Moderação aprova a imagem | `{ CloudinaryPublicId, Referencia }` |
| `ImagemRejeitada` | Moderação rejeita a imagem | `{ CloudinaryPublicId, Referencia }` |

---

## Entidades Envolvidas

### `Usuario` (perfil do Prestador)

**Tabela**: `usuarios`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `name` | Nome exibido publicamente |
| `FotoPerfilUrl` | `string?` | `profile_photo_url` | URL Cloudinary |
| `Slug` | `string?` | `slug` | Único global; **imutável após publicação** |
| `Descricao` | `string?` | `description` | Bio pública |
| `Especialidade` | `string?` | `specialty` | Texto livre (legacy) |
| `CidadeId` | `Guid?` | `city_id` | FK → `cidades.id` |
| `MediaAvaliacoes` | `decimal` | `rating_average` | Calculado via Job SY-07 |
| `TotalAvaliacoes` | `int` | `rating_count` | Contagem |

### `ImagemPortfolio`

**Tabela**: `imagens_portfolio`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `UsuarioId` | `Guid` | `user_id` | FK → `usuarios.id` |
| `Url` | `string` | `url` | URL pública Cloudinary |
| `CloudinaryPublicId` | `string` | `cloudinary_public_id` | Para deleção |
| `Moderada` | `bool` | `moderated` | Default `false` |
| `Aprovada` | `bool?` | `approved` | `null` = pendente, `true` = ok, `false` = rejeitada |
| `Ordem` | `int` | `display_order` | Ordena exibição |
| `CriadoEm` | `DateTime` | `created_at` | UTC |
| `DeletadoEm` | `DateTime?` | `deleted_at` | Soft delete |

### `CategoriaUsuario`

**Tabela**: `usuarios_categorias`

| Propriedade | Tipo C# | Coluna DB |
|------------|---------|-----------|
| `UsuarioId` | `Guid` | `user_id` |
| `CategoriaId` | `Guid` | `category_id` |

### `CidadeUsuario`

**Tabela**: `usuarios_cidades`

| Propriedade | Tipo C# | Coluna DB |
|------------|---------|-----------|
| `UsuarioId` | `Guid` | `user_id` |
| `CidadeId` | `Guid` | `city_id` |

### `Categoria`

**Tabela**: `categorias`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `name` | Ex: "Encanador" |
| `Slug` | `string` | `slug` | Ex: `encanador` |
| `Ativa` | `bool` | `active` | Inativas não aparecem na busca |
| `Ordem` | `int` | `display_order` | |

### `Cidade`

**Tabela**: `cidades`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `name` | Ex: "Itapevi" |
| `Estado` | `string` | `state` | Sigla UF |
| `Slug` | `string` | `slug` | Ex: `itapevi` |
| `Ativa` | `bool` | `active` | |

---

## API Endpoints

| Método | Rota | Auth | Caso de Uso |
|--------|------|------|-------------|
| `GET` | `/{cidadeSlug}/{categoriaSlug}/{slug}` | Público | FP-01 |
| `PUT` | `/api/auth/perfil` | Bearer (Prestador) | FP-04 |
| `POST` | `/api/auth/portfolio/url-assinada` | Bearer (Prestador) | FP-03 (passo 2) |
| `POST` | `/api/auth/portfolio` | Bearer (Prestador) | FP-03 (passo 4) |
| `DELETE` | `/api/auth/portfolio/{id}` | Bearer (Prestador) | Remoção de imagem |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Página de perfil acessível sem autenticação via `/{cidadeSlug}/{categoriaSlug}/{slug}`. |
| CA-02 | Imagens de portfólio exibidas apenas quando `Aprovada = true`. |
| CA-03 | Avaliações listadas com nota, comentário e data. |
| CA-04 | Botão "Contratar" redireciona visitante não autenticado para `/entrar`. |
| CA-05 | Botão "Contratar" inicia fluxo de solicitação para Cliente autenticado (RF-04). |
| CA-06 | Edição de perfil não altera o `Slug` mesmo que o campo seja enviado no payload. |
| CA-07 | Upload de imagem maior que 10 MB é rejeitado. |
| CA-08 | CPF, e-mail e dados bancários **nunca aparecem** no perfil público. |
| CA-09 | Imagem rejeitada pela moderação não é exibida e é removida do Cloudinary. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Acesso ao perfil público sem autenticação | `200 OK`, dados do prestador retornados |
| CT-02 | Slug inexistente | `404 Not Found` |
| CT-03 | Upload de imagem válida | Imagem criada com `Aprovada = null`; exibida após aprovação |
| CT-04 | Upload de imagem > 10 MB | Rejeitado antes de chamar Cloudinary |
| CT-05 | Moderação aprova imagem | `Aprovada = true`, imagem aparece no perfil |
| CT-06 | Moderação rejeita imagem | `Aprovada = false`, imagem não aparece, arquivo removido |
| CT-07 | Prestador edita nome | Nome atualizado, Slug inalterado |
| CT-08 | Prestador envia `slug` diferente no PUT de perfil | Slug ignorado, permanece o original |
| CT-09 | Perfil sem imagens aprovadas | Galeria exibida vazia, sem erro |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Slug mutável após publicação | — | ADR-09 define slug como imutável. Permite alteração de nome exibido, não do slug. |
| Contato direto via telefone exibido no perfil | RF-02 PDF | Arquitetura não expõe telefone publicamente (LGPD). Contato ocorre via chat interno (RF-06). |
| Moderação manual de imagens na publicação inicial | — | Moderação é automática via Cloudinary. Revisão manual existe apenas como fallback para Admin (AD-08). |
