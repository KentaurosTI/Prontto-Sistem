# Prontto — Checklist de QA e Documentação de Teste

**Versão:** rodada de correções/features de 07/2026 — **consolidado com progresso do QA**
**Ambiente de produção:**
- Frontend: `https://prontto.org`
- API: `https://api.prontto.org` (health: `https://api.prontto.org/healthz` → 200)

**Legenda:** `[x]` = validado pelo QA · `[ ]` = pendente / não testado. Os casos das seções **5A** e **7A** e alguns bullets novos (5.1/5.3) entraram após o último ciclo do QA — ainda pendentes.

**Como usar este documento:** execute cada caso na ordem. Marque `[x]` quando passar. Em caso de falha, anote a URL, o passo, o que esperava e o que aconteceu (com print e, se possível, o console do navegador — F12 → Console/Network).

---

## 0. Pré-requisitos e contas de teste

- [x] Testar em **navegador atualizado** (Chrome/Edge/Firefox) e também em **janela anônima** para descartar cache.
- [x] Após cada deploy do front, fazer **Ctrl+Shift+R** (hard refresh).
- [x] Testar em **desktop** (≥1024px) e **mobile** (≤480px) — o layout é responsivo.
- [x] Ter à disposição 3 perfis:
  - **Cliente (contratante)** — conta com `tipoConta = cliente`.
  - **Prestador** — conta com `tipoConta = prestador`, com cidade de atuação = São Paulo.
  - **Admin** — conta com papel `admin`.

> Observação: como a plataforma agora atende **somente o estado de SP**, cadastre/escolha cidades de SP nos testes.

---

## 1. Cadastro e Login (telas de acesso)

### 1.1 Tela de Login (`/entrar`)
- [x] Layout em **painel dividido**: painel escuro à esquerda (desktop) + formulário à direita.
- [x] Campo de senha tem botão de **mostrar/ocultar** (olho).
- [x] Login com credenciais válidas → redireciona (cliente/prestador → `/minha-area`; admin → `/admin`).
- [x] Login inválido → mensagem de erro visível.
- [x] Link **"Cadastre-se grátis"** leva a `/cadastrar`.
- [x] Mobile: o painel escuro some e o formulário ocupa a tela.

### 1.2 Tela de Cadastro (`/cadastrar`)
- [x] Página **única** (sem etapas) com os cards **"Cliente" / "Prestador"** no topo.
- [x] Selecionar **Prestador** faz aparecer o campo **"Especialidade principal"**.
- [x] Selecionar **Cliente** oculta a especialidade.
- [x] Cadastro válido cria a conta e já entra logado.
- [x] Validações: nome curto, e-mail inválido e senha < 8 caracteres exibem erro.
- [x] Link **"Entrar"** leva a `/entrar`.

### 1.3 Sessão e Logout
- [x] Após login, permanecer **mais de 15 minutos** na sessão e navegar → **não é deslogado** (o token é renovado automaticamente).
- [x] Clicar em **"Sair"** no header → limpa a sessão e **redireciona para `/entrar`** (não trava).
- [x] Após logout, tentar abrir `/minha-area` diretamente → vai para login.

---

## 2. Header e Notificações

- [x] **Deslogado:** o header mostra "Seja um profissional", "Como funciona?", "Segurança" e "Entrar".
- [x] **Logado:** esses links de marketing **somem**; aparecem **sino de notificações**, "Minha área", "Sair" (e "Admin" se for admin).
- [x] Clicar no **logo PRONTTO** (em qualquer tela, incluindo Admin) → volta para a home `/`.

### 2.1 Sino de notificações
- [x] O **sino** aparece no header quando logado.
- [ ] Com notificações não lidas, mostra um **badge vermelho** com a contagem (ex.: `1`, `9+`).
- [ ] Clicar no sino abre um **dropdown** com a lista (título, mensagem, data).
- [ ] **Gerar notificação:** com o Cliente e o Prestador de um mesmo serviço, um envia uma **mensagem no chat** → o **outro recebe** notificação "Nova mensagem" (o badge incrementa; pode levar até ~30s para atualizar sozinho, ou reabrir o sino).
- [ ] Clicar numa notificação → **abre o serviço** correspondente e marca como lida (badge diminui).
- [ ] Botão **"Marcar todas como lidas"** zera o badge.

---

## 3. Fluxo do Contratante (escolher serviço → prestador → solicitar)

### 3.1 Descoberta
- [x] Na home, escolher uma categoria (ex.: Jardinagem) → abre a página da categoria.
- [x] Na página da categoria existe o botão **"Ver profissionais de {categoria}"** → leva a `/prestadores/{categoria}`.

### 3.2 Listagem de prestadores (`/prestadores/:categoria`)
- [x] Título "Profissionais de {categoria}" e subtítulo "Escolha um profissional e envie sua solicitação".
- [x] Filtro **Cidade** lista **apenas cidades de SP**.
- [ ] **Logado como cliente com cidade definida:** a listagem já vem **filtrada pela cidade do cliente** (proximidade).
- [x] Cada card mostra: avatar/foto, nome, especialidade, **nota (estrelas)** e cidade.
- [x] Trocar a cidade no filtro atualiza a lista (e reflete o filtro na URL).
- [x] Sem prestadores na cidade → estado vazio "Nenhum profissional encontrado".
- [x] Clicar num card → abre o **perfil do prestador**.

### 3.3 Perfil do prestador (`/prestador/:slug`)
- [x] Avatar, nome, especialidade, **nota e nº de avaliações**, categorias e cidades atendidas.
- [ ] Seções **Sobre**, **Portfólio** (fotos carregam) e **Avaliações**.
- [x] **Como cliente:** botão **"Contratar serviço"** visível.
- [x] **Como prestador logado:** em vez do botão, aparece o aviso "Você está logado como prestador. Para contratar, use uma conta de cliente."
- [ ] As **fotos do portfólio e o avatar carregam** (não aparecem quebradas).

### 3.4 Solicitar serviço (`/servicos/novo`)
- [x] Vindo do perfil, aparece o banner **"Prestador selecionado: {nome}"**.
- [x] Campos: Título*, Descrição*, Categoria*, **Tipo de serviço (subcategoria)** aparece conforme a categoria, Cidade, Data desejada, Endereço.
- [x] Enviar com campos obrigatórios vazios → validações visíveis.
- [ ] **Proximidade:** escolher um prestador que **não atende a cidade** informada e enviar → bloqueio com mensagem "O profissional escolhido não atende a cidade informada...".
- [ ] Enviar válido → cria o serviço (status **Em negociação**) e vai para `/minha-area`.
- [ ] O **prestador recebe** uma notificação "Nova solicitação de serviço".

---

## 4. Detalhe do Serviço e Chat (`/servico/:id`)

- [ ] Cabeçalho: botão **← Voltar**, título e **badge de status legível** (ex.: "Em negociação" — **não** "EmNegociacao").
- [ ] Cartões **DETALHES** e **PARTICIPANTES** preenchidos.
- [ ] Seção **AÇÕES** mostra os botões conforme o papel/status (ex.: Cliente em negociação vê **"Cancelar Serviço"**; prestador vê "Vincular-me"/"Marcar como concluído" quando aplicável).
- [ ] **Chat:** enviar uma mensagem → aparece **uma única vez** (sem duplicação, mesmo após o polling de 10s).
- [ ] Enviar proposta de preço (prestador) → aparece o card de proposta; cliente consegue aceitar.

### 4.1 Privacidade do endereço (regra de negócio)
- [ ] **Como prestador**, antes da aprovação (status Em negociação / Aguardando pagamento): o **Endereço** e o **Agendamento** aparecem como **"🔒 Revelado após a aprovação"** (não mostra o endereço real).
- [ ] **Como cliente:** o endereço e o horário aparecem normalmente.
- [ ] Após a aprovação/pagamento (status Pago em diante): o **prestador passa a ver** o endereço e o horário reais.

---

## 5. Minha Área — Prestador (`/minha-area`)

### 5.1 Perfil Público (aba)
- [x] Sidebar com **avatar** (mostra a foto se houver, senão a inicial), nome, e-mail e selo "Prestador".
- [ ] Campos: Sobre você, Especialidade, **Foto de perfil (upload)**, Categorias e Cidades (checkboxes — **só SP**).
- [ ] **Upload de foto:** botão **"Enviar foto"** → seleciona arquivo → **preview aparece**; botão **"Remover"** limpa.
- [x] Salvar perfil → mensagem "Perfil atualizado com sucesso!" e o **avatar do sidebar atualiza** com a foto.
- [ ] **Dar F5** na página → as informações salvas (descrição, especialidade, foto, categorias, cidades) **permanecem** (não voltam em branco).
- [ ] *(novo)* Marcar **2+ categorias** de atuação e trocar/adicionar **cidade(s)** → Salvar → abrir **"Ver perfil público"** → as novas categorias/cidades aparecem **imediatamente** (sem esperar minutos).

### 5.2 Dados Bancários (aba)
- [ ] Formulário de chave Pix salva e recarrega corretamente.

### 5.3 Portfólio (aba)
- [ ] Grade com o bloco **"＋ Adicionar foto"**.
- [ ] Selecionar uma imagem → **envia na hora** e aparece como card na grade.
- [ ] Também é possível **arrastar e soltar** uma imagem na área da grade.
- [ ] Cada foto tem o botão de **excluir** (lixeira) no canto; excluir remove a foto.
- [ ] As fotos **não aparecem quebradas** (carregam da API), tanto na Minha Área quanto no **perfil público**.
- [ ] *(novo)* Subir uma foto nova → dar **F5** e conferir no **perfil público** → a foto **persiste** (não some após atualizar/deploy).
- [ ] Foto > 5 MB ou tipo inválido → mensagem de erro.

### 5.4 Meus Serviços (aba)
- [ ] Lista **compacta** (`ms-item`): título + info (cliente · categoria · data · valor) numa linha, com **badge de status colorido** à direita.
- [ ] Link **"Ver detalhes →"** abre o serviço.
- [ ] Serviço concluído (visão cliente) mostra o botão **"Avaliar"** e o formulário de avaliação.

> Observação: a **Minha Área** é usada tanto por prestador ("Meus Serviços") quanto por cliente ("Meus Pedidos"), com o rótulo mudando conforme o tipo de conta.

---

## 5A. Minha Área — Cliente (`/minha-area`)  *(novo — pendente de QA)*

- [ ] Logado como **cliente**, a Minha Área abre na aba **"Meu Perfil"** (além de "Meus Pedidos").
- [ ] **Meu Perfil** tem os campos: **Nome completo**, **Telefone** (opcional), **Cidade** (select — **só SP**) e **Endereço**.
- [ ] Preencher e **Salvar** → mensagem "Cadastro atualizado com sucesso!".
- [ ] **Dar F5** → os dados permanecem preenchidos.
- [ ] O **nome** salvo reflete no cabeçalho/sidebar.
- [ ] Ir em **"Solicitar serviço"** → os campos **Cidade** e **Endereço** já vêm **pré-preenchidos** com o cadastro do cliente.
- [ ] A aba **"Meus Pedidos"** continua listando os pedidos normalmente.

---

## 6. Painel Admin (`/admin`)

- [ ] **Layout alinhado:** sidebar escura **encostada na borda esquerda** (sem vão branco à esquerda) e conteúdo ocupando o resto, **sem cortar os cards da direita**, em telas largas e estreitas.
- [x] Sidebar com **ícones de linha** (não emojis) e o selo "ADMIN".
- [x] Clicar no logo **PRONTTO** → volta para a home.
- [ ] **Visão Geral:** 6 cards de KPI (Total de Usuários, Total de Serviços, Receita Total, Serviços em andamento, Prestadores ativos, Receita pendente) com **ícones de linha coloridos**.
- [x] Clicar nos 3 primeiros cards navega para a aba correspondente (Usuários / Serviços / Financeiro).
- [ ] **Aba Serviços:** grade com status legível; menu de ações **"…"** no fim da linha (editar/excluir).
- [x] **Aba Usuários:** grade com menu de ações **"…"** (editar/excluir; sem "adicionar").
- [ ] **Aba Financeiro:** valores exibidos corretamente.
- [ ] Editar/excluir um serviço ou usuário reflete na listagem após confirmar.

---

## 7. Regras gerais e regressões

- [x] **Cidades = só SP:** em todos os selects/checkboxes de cidade, aparecem apenas cidades de SP (São Paulo, Guarulhos, Osasco, Santo André, Campinas, etc.).
- [x] **LGPD:** no primeiro acesso aparece o **banner de cookies** ("Aceitar todos" / "Apenas essenciais"); a escolha persiste.
- [ ] **Responsivo:** em mobile, o menu vira "hambúrguer"; as telas do fluxo se ajustam; nenhuma tela rola horizontalmente.
- [x] **Rodapé:** links institucionais funcionam (Quem Somos, Trabalhe Conosco, Blog/Dicas, Segurança/Termos, Ajuda, Política de Privacidade, Termos de Uso).
- [x] **404:** acessar uma URL inexistente mostra a página de "não encontrado".

---

## 7A. Navegação, menus e institucional (regressões desta rodada)  *(novo — pendente de QA)*

- [ ] **Mobile — mega-menu:** abrir uma **categoria** na barra de categorias → tocar em uma **subcategoria** → o submenu **recolhe** e navega (não fica aberto atrapalhando).
- [ ] O mega-menu também fecha ao tocar no título de um grupo ou no botão **"Fazer um pedido"**.
- [ ] **Botão "Voltar ao início":** em `/termos` (rodapé → Institucional → **Segurança**) e em `/privacidade`, o botão no fim da página tem **texto branco legível** (não some no fundo laranja) e leva à home.
- [ ] **Fluxo de contratação (regressão):** contratar via perfil do prestador **finaliza** o pedido (não dá "Erro ao criar solicitação"); com cidade fora da área do prestador, aparece a **mensagem de bloqueio específica**.

---

## 8. Limitações conhecidas / fora de escopo (não são bugs)

- **Pagamentos (Stripe/Pagar.me):** a integração de pagamento não faz parte desta rodada; o fluxo de PIX exibe dados quando existem, mas a captura real de pagamento não está ativa.
- **Matching por proximidade = nível cidade:** a compatibilidade é por **cidade** de SP (não por bairro/raio em km). Bairro/geolocalização ficam para uma evolução futura.
- **Uploads antigos:** imagens enviadas **antes** da correção de armazenamento persistente foram perdidas num deploy anterior; a partir de agora os uploads são preservados entre deploys.
- **Status inicial do pedido:** ao criar a solicitação direcionada a um prestador, ela nasce em **"Em negociação"** aguardando o prestador se vincular/enviar proposta.

---

## 9. Matriz rápida de status do serviço (para conferência de badges)

| Interno (API) | Rótulo exibido |
|---|---|
| em_negociacao | Em negociação |
| aguardando_pagamento | Aguardando pagamento |
| pago | Pago |
| em_andamento | Em andamento |
| aguardando_confirmacao_cliente | Aguardando confirmação |
| em_disputa | Em disputa |
| concluido | Concluído |
| cancelado | Cancelado |

> Se algum badge aparecer em "CamelCase" (ex.: `EmNegociacao`), é bug — reportar.

---

## 10. Modelo para reporte de bug

```
Título:
Ambiente: produção / navegador + versão / desktop|mobile
Conta usada: cliente | prestador | admin
URL:
Passos para reproduzir:
1.
2.
Resultado esperado:
Resultado obtido:
Evidência: (print + console F12 se houver erro)
Severidade: bloqueante | alta | média | baixa
```

---

## 11. Resumo do progresso do QA (último ciclo)

- **Concluídas 100%:** seções 0, 1 (1.1/1.2/1.3), 2 (header), 3.1, 3.2 (6/7).
- **Parciais:** 2.1 (sino), 3.3, 3.4, 5.1, 6 (admin), 7.
- **Ainda não iniciadas:** 4 (detalhe/chat), 4.1 (privacidade), 5.2/5.3/5.4, **5A** (perfil do cliente), **7A** (navegação/regressões).
- **Falhas reportadas:** nenhuma marcada como falha no último ciclo — os `[ ]` são pendências de teste. Se algum for falha, usar o modelo da seção 10.
