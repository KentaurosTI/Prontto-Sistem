# RF-03 — Busca e Filtragem de Prestadores

**Versão**: 1.1
**Fonte da verdade**: `ARCHITECTURE.md` v1.1 (2026-06-03)
**Status**: Revisado — divergências do PDF original corrigidas

---

## Objetivo

Permitir que qualquer visitante (autenticado ou não) descubra Prestadores por categoria de serviço e cidade, a partir de uma grade de categorias na tela inicial ou de buscas diretas.

---

## Descrição

A tela inicial da plataforma exibe uma grade de categorias como ponto de entrada da descoberta. Ao selecionar uma categoria (e opcionalmente uma cidade), o sistema retorna Prestadores correspondentes de forma paginada.

A busca é **pública** — não exige autenticação. Qualquer visitante pode explorar categorias e visualizar perfis de Prestadores. O login é exigido apenas ao clicar em "Contratar" (que pertence ao RF-04).

Categorias e cidades são **entidades canônicas** (`Categoria`, `Cidade`) referenciadas por FK — nunca texto livre. A URL pública segue o padrão `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`.

---

## Atores

| Ator | Descrição |
|------|-----------|
| Visitante | Qualquer usuário (autenticado ou não) que navega pela plataforma |
| Cliente | Usuário com `TipoConta = Cliente` que pode iniciar solicitação a partir dos resultados |
| Sistema | Executa a busca com filtros, retorna lista paginada |

---

## Pré-condições

- Pelo menos uma `Categoria` ativa (`Ativa = true`) deve existir no catálogo.
- Pelo menos uma `Cidade` ativa deve existir no catálogo.
- Para que um Prestador apareça na busca: perfil completo (Slug gerado), pelo menos uma categoria e uma cidade associadas.

---

## Fluxos Principais

### FP-01 — Navegação pela Grade de Categorias

1. Visitante acessa a tela inicial (`/`).
2. Sistema retorna lista de `Categoria` com `Ativa = true`, ordenadas por `Ordem`.
3. Frontend exibe grade de categorias.
4. Visitante clica em uma categoria.
5. Frontend navega para a tela de resultados com filtro de categoria pré-aplicado (FP-02).

### FP-02 — Busca de Prestadores por Categoria e Cidade

1. Visitante acessa a tela de busca com `categoriaSlug` (obrigatório) e `cidadeSlug` (opcional).
2. Sistema executa query em `usuarios_categorias` (JOIN `categorias`, JOIN `usuarios`) e `usuarios_cidades` (JOIN `cidades`):
   - Filtra `TipoConta = Prestador`
   - Filtra `Slug IS NOT NULL` (perfil completo)
   - Filtra `DeletadoEm IS NULL`
   - Filtra `categorias.slug = categoriaSlug`
   - Se `cidadeSlug` informado: filtra `cidades.slug = cidadeSlug`
3. Sistema retorna lista paginada (`page`, `pageSize` máximo 50) com:
   - `Id`, `Nome`, `FotoPerfilUrl`, `MediaAvaliacoes`, `TotalAvaliacoes`, `Slug`, `CidadeId`, `CategoriaId`
4. Frontend exibe card para cada Prestador com nome, foto, nota média e cidade.
5. Visitante clica em um card → navega para o perfil público do Prestador (RF-02, FP-01).

### FP-03 — Visualização do Perfil a partir da Busca

1. Visitante clica em um Prestador nos resultados.
2. Frontend navega para `/{cidadeSlug}/{categoriaSlug}/{slugPrestador}`.
3. Ver RF-02, FP-01.

---

## Fluxos Alternativos

### FA-01 — Nenhum Prestador encontrado para a combinação

- **Ponto de desvio**: Passo 2 do FP-02 — query retorna 0 resultados.
- **Ação**: Sistema retorna `200 OK` com lista vazia e `totalCount = 0`.
- **Continuação**: Frontend exibe mensagem "Nenhum prestador encontrado para esta categoria e cidade."

### FA-02 — Categoria ou Cidade inválida/inativa

- **Ponto de desvio**: Passo 2 do FP-02 — `categoriaSlug` ou `cidadeSlug` não encontrado na tabela ou `Ativa = false`.
- **Ação**: Sistema retorna `404 Not Found`.
- **Continuação**: Frontend exibe erro ou redireciona para a grade de categorias.

### FA-03 — Visitante não autenticado clica em "Contratar"

- **Ponto de desvio**: No card de Prestador, visitante sem autenticação clica em "Contratar".
- **Ação**: Frontend redireciona para `/entrar`.
- **Continuação**: Após login, Cliente pode iniciar solicitação. **A navegação de busca em si não é bloqueada.**

---

## Fluxos de Exceção

### FE-01 — `pageSize` maior que 50

- **Ponto de desvio**: Parâmetro de paginação excede limite.
- **Ação**: Backend aplica o máximo permitido (`pageSize = 50`) silenciosamente ou retorna `400`.
- **Continuação**: Requisição processada com o limite correto.

---

## Pós-condições

- Lista de Prestadores filtrada retornada com paginação.
- Nenhum dado sensível (CPF, e-mail, dados bancários) exposto nos resultados.

---

## Regras de Negócio

| ID | Regra |
|----|-------|
| RN-01 | A busca de Prestadores é **pública** — não exige autenticação. Qualquer visitante pode buscar e ver perfis. |
| RN-02 | Categorias e cidades são **entidades canônicas** com FK. Nunca aceitar string livre como filtro — sempre usar `categoriaSlug` para lookup do `CategoriaId`. |
| RN-03 | Somente Prestadores com `Slug IS NOT NULL` (perfil completo publicado), `DeletadoEm IS NULL` e `TipoConta = Prestador` aparecem nos resultados. |
| RN-04 | Somente categorias com `Ativa = true` aparecem na grade e nos filtros. Somente cidades com `Ativa = true` aparecem nos filtros. |
| RN-05 | Os resultados da busca exibem: nome, foto, nota média, cidade — nunca CPF, e-mail ou dados bancários. |
| RN-06 | A lista de categorias canônicas é cacheada em memória por **1 hora** (`IMemoryCache`), invalidada ao alterar. |
| RN-07 | O perfil público do Prestador cacheado em memória por **5 minutos** por slug. |
| RN-08 | Paginação: parâmetros `page` (base 1) e `pageSize` (máximo 50). Cursor-based pagination não se aplica à busca — apenas ao chat. |

---

## Eventos de Domínio

Não há eventos de domínio publicados por este RF. A busca é uma operação de leitura pura.

---

## Entidades Envolvidas

### `Categoria`

**Tabela**: `categorias`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `name` | Ex: "Encanador" |
| `Slug` | `string` | `slug` | Único; kebab-case. Ex: `encanador` |
| `Ativa` | `bool` | `active` | Inativas não aparecem na busca |
| `Ordem` | `int` | `display_order` | Ordena exibição na grade |

### `Cidade`

**Tabela**: `cidades`

| Propriedade | Tipo C# | Coluna DB | Observações |
|------------|---------|-----------|------------|
| `Id` | `Guid` | `id` | PK |
| `Nome` | `string` | `name` | Ex: "Itapevi" |
| `Estado` | `string` | `state` | Sigla UF. Ex: "SP" |
| `Slug` | `string` | `slug` | Único; kebab-case |
| `Ativa` | `bool` | `active` | Inativas não aparecem nos filtros |

### `CategoriaUsuario`

**Tabela**: `usuarios_categorias`

| Propriedade | Tipo C# | Coluna DB |
|------------|---------|-----------|
| `UsuarioId` | `Guid` | `user_id` |
| `CategoriaId` | `Guid` | `category_id` |

**Índice**: `category_id` (B-tree) — busca por categoria

### `CidadeUsuario`

**Tabela**: `usuarios_cidades`

| Propriedade | Tipo C# | Coluna DB |
|------------|---------|-----------|
| `UsuarioId` | `Guid` | `user_id` |
| `CidadeId` | `Guid` | `city_id` |

**Índice**: `city_id` (B-tree) — busca por cidade

### `Usuario` (projeção de resultado)

Campos retornados pela busca: `Id`, `Nome`, `FotoPerfilUrl`, `Slug`, `MediaAvaliacoes`, `TotalAvaliacoes`.

---

## API Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/categorias` | Público | Lista categorias ativas (cacheado 1h) |
| `GET` | `/api/cidades` | Público | Lista cidades ativas |
| `GET` | `/api/prestadores?categoriaSlug=&cidadeSlug=&page=&pageSize=` | Público | Busca paginada de prestadores |

---

## Critérios de Aceitação

| ID | Critério |
|----|----------|
| CA-01 | Grade de categorias exibida na tela inicial com todas as categorias `Ativa = true`. |
| CA-02 | Busca retorna apenas Prestadores com perfil completo (`Slug IS NOT NULL`) e não deletados. |
| CA-03 | Busca por `categoriaSlug` inválido retorna `404`. |
| CA-04 | Visitante não autenticado pode buscar e ver resultados sem ser bloqueado ou redirecionado. |
| CA-05 | Resultado exibe nome, foto, nota média e cidade de cada Prestador. |
| CA-06 | CPF, e-mail e dados bancários nunca aparecem nos resultados. |
| CA-07 | Paginação funciona corretamente com `pageSize` máximo de 50. |
| CA-08 | Lista de categorias é servida do cache (sem query ao banco) dentro do TTL de 1 hora. |

---

## Casos de Teste Funcionais

| ID | Cenário | Resultado Esperado |
|----|---------|-------------------|
| CT-01 | Acesso à listagem de categorias sem autenticação | `200 OK`, lista de categorias ativas |
| CT-02 | Busca por `categoriaSlug` válido | `200 OK`, lista paginada de Prestadores daquela categoria |
| CT-03 | Busca por `categoriaSlug` + `cidadeSlug` válidos | `200 OK`, lista filtrada por categoria e cidade |
| CT-04 | Busca por `categoriaSlug` inexistente | `404 Not Found` |
| CT-05 | Busca sem resultados | `200 OK`, lista vazia, `totalCount = 0` |
| CT-06 | `pageSize = 100` (acima do limite) | Limitado a 50 ou `400 Bad Request` |
| CT-07 | Prestador com perfil incompleto (`Slug IS NULL`) | Não aparece nos resultados |
| CT-08 | Prestador com `DeletadoEm` preenchido | Não aparece nos resultados |

---

## Escopo Negativo (o que NÃO está na arquitetura)

| Item | Origem | Motivo da Exclusão |
|------|--------|-------------------|
| Bloqueio de visitante não autenticado na busca | RF-03 PDF | Incorreto. Arquitetura §10.2 e caso de uso CL-04 definem busca como pública ("Sem autenticação"). |
| Pesquisa textual por serviço (fulltext search) | RF-03 PDF | Mencionado no PDF como "futuramente". Não implementado na arquitetura atual. |
| Filtro por região "bairro" | RF-03 PDF | Arquitetura usa `Cidade` como granularidade de região. Bairro não é uma entidade do domínio. |
| Ordenação por proximidade geográfica | — | Não documentado na arquitetura. |
