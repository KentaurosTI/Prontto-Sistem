// Seeder de dados de teste — Prontto
// Uso: dotnet run [dev|prod]   (padrão: dev)

using MySqlConnector;

var ambiente = args.Length > 0 ? args[0] : "dev";

var connString = ambiente == "prod"
    ? "Server=72.60.122.109;Database=u638238509_PronttoAdm;User=u638238509_PronttoAdm;Password=>r@OeHY0X3;Port=3306;AllowPublicKeyRetrieval=true;SslMode=Required;Connection Timeout=30;"
    : "Server=72.60.122.109;Database=u638238509_prontto_dev;User=u638238509_prontto_dev;Password=^ybtnwO?hPc8;Port=3306;AllowPublicKeyRetrieval=true;SslMode=Required;Connection Timeout=30;";

Console.WriteLine($"╔══════════════════════════════════════════╗");
Console.WriteLine($"║   Prontto — Seeder de Dados de Teste     ║");
Console.WriteLine($"╚══════════════════════════════════════════╝");
Console.WriteLine($"Ambiente: {ambiente.ToUpper()}");
Console.WriteLine("Conectando...");

await using var conn = new MySqlConnection(connString);
await conn.OpenAsync();
Console.WriteLine("Conectado!\n");

string NewId() => Guid.NewGuid().ToString();
string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
string Ago(int dias) => DateTime.UtcNow.AddDays(-dias).ToString("yyyy-MM-dd HH:mm:ss.ffffff");
string AgoH(int horas) => DateTime.UtcNow.AddHours(-horas).ToString("yyyy-MM-dd HH:mm:ss.ffffff");
string Em(int dias) => DateTime.UtcNow.AddDays(dias).ToString("yyyy-MM-dd HH:mm:ss.ffffff");
string HashSenha(string s) => BCrypt.Net.BCrypt.HashPassword(s, workFactor: 12);

// ─── Categorias ───────────────────────────────────────────────────────────────
Console.WriteLine("▶ Buscando categorias...");
var categorias = new List<(string id, string slug, string nome)>();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT id, slug, nome FROM categorias WHERE ativo = 1 ORDER BY ordem_exibicao";
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        categorias.Add((r.GetGuid("id").ToString(), r.GetString("slug"), r.GetString("nome")));
}
if (categorias.Count == 0) { Console.WriteLine("[ERRO] Nenhuma categoria encontrada. Rode o seeder principal primeiro."); return; }
Console.WriteLine($"  {categorias.Count} categorias encontradas.");

string GetCat(string parcial) =>
    categorias.FirstOrDefault(c => c.slug.Contains(parcial) || c.nome.ToLower().Contains(parcial)).id
    ?? categorias[0].id;

// ─── Cidades ──────────────────────────────────────────────────────────────────
Console.WriteLine("▶ Buscando cidades...");
var cidades = new List<(string id, string slug, string nome)>();
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT id, slug, nome FROM cidades WHERE ativo = 1";
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        cidades.Add((r.GetGuid("id").ToString(), r.GetString("slug"), r.GetString("nome")));
}
Console.WriteLine($"  {cidades.Count} cidades encontradas.");

string GetCid(string parcial) =>
    cidades.FirstOrDefault(c => c.slug.Contains(parcial) || c.nome.ToLower().Contains(parcial)).id
    ?? cidades[0].id;

// ─── Usuários ─────────────────────────────────────────────────────────────────
Console.WriteLine("\n▶ Criando usuários...");
var senhaHash = HashSenha("Senha123");
var senhaAdminHash = HashSenha("Admin123");
var uids = new Dictionary<string, string>(); // email → id

async Task<string> UpsertUsuario(string nome, string email, string tipo, string papel,
    string? especialidade, string cidParcial, string slug, string? descricao,
    string? senhaCustom = null)
{
    await using var chk = conn.CreateCommand();
    chk.CommandText = "SELECT id FROM usuarios WHERE email = @e";
    chk.Parameters.AddWithValue("@e", email);
    var existId = (await chk.ExecuteScalarAsync())?.ToString();
    if (existId != null) { Console.WriteLine($"  [skip] {email}"); return existId; }

    var id = NewId();
    var hash = senhaCustom ?? senhaHash;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO usuarios
        (id, nome, email, hash_senha, tipo_conta, papel, especialidade, cidade_id,
         slug, descricao, media_avaliacoes, total_avaliacoes, criado_em, atualizado_em)
        VALUES (@id,@nome,@email,@hash,@tipo,@papel,@esp,@cid,@slug,@desc,0.00,0,@now,@now)";
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@nome", nome);
    cmd.Parameters.AddWithValue("@email", email);
    cmd.Parameters.AddWithValue("@hash", hash);
    cmd.Parameters.AddWithValue("@tipo", tipo);
    cmd.Parameters.AddWithValue("@papel", papel);
    cmd.Parameters.AddWithValue("@esp", (object?)especialidade ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@cid", GetCid(cidParcial));
    cmd.Parameters.AddWithValue("@slug", slug);
    cmd.Parameters.AddWithValue("@desc", (object?)descricao ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [ok] {nome} ({tipo})");
    return id;
}

uids["ana.souza@email.com"]        = await UpsertUsuario("Ana Souza",       "ana.souza@email.com",        "Cliente",   "Usuario", null,              "paulo",    "ana-souza",       null);
uids["bruno.lima@email.com"]       = await UpsertUsuario("Bruno Lima",      "bruno.lima@email.com",       "Cliente",   "Usuario", null,              "janeiro",  "bruno-lima",      null);
uids["carla.mendes@email.com"]     = await UpsertUsuario("Carla Mendes",    "carla.mendes@email.com",     "Cliente",   "Usuario", null,              "horizonte","carla-mendes",    null);
uids["mariana.limpeza@email.com"]  = await UpsertUsuario("Mariana Costa",   "mariana.limpeza@email.com",  "Prestador", "Usuario", "Limpeza residencial e comercial", "paulo",    "mariana-costa",   "Especialista em limpeza com 6 anos de experiência. Atendo residências, escritórios e pós-obra.");
uids["joao.encanador@email.com"]   = await UpsertUsuario("João Ferreira",   "joao.encanador@email.com",   "Prestador", "Usuario", "Encanamento e hidráulica",        "janeiro",  "joao-ferreira",   "Encanador com 8 anos de experiência. Conserto de vazamentos, instalações e reformas hidráulicas.");
uids["fernanda.pintura@email.com"] = await UpsertUsuario("Fernanda Alves",  "fernanda.pintura@email.com", "Prestador", "Usuario", "Pintura residencial e comercial", "horizonte","fernanda-alves",  "Pintora profissional. Residencial, comercial, texturas e pinturas decorativas.");
uids["carlos.eletricista@email.com"]= await UpsertUsuario("Carlos Silva",   "carlos.eletricista@email.com","Prestador","Usuario", "Instalações elétricas",           "janeiro",  "carlos-silva",    "Eletricista com 10 anos de experiência. Tomadas, disjuntores, instalações completas e laudos.");
uids["admin@prontto.org"]          = await UpsertUsuario("Admin Prontto",   "admin@prontto.org",          "Cliente",   "Admin",   null,              "paulo",    "admin-prontto",   null, senhaCustom: senhaAdminHash);

// ─── Dados bancários ──────────────────────────────────────────────────────────
Console.WriteLine("\n▶ Criando dados bancários...");

async Task UpsertDadosBancarios(string email, string tipoChave, string chave, string nome, string cpf)
{
    if (!uids.TryGetValue(email, out var uid)) return;
    await using var chk = conn.CreateCommand();
    chk.CommandText = "SELECT COUNT(*) FROM dados_bancarios WHERE usuario_id = @uid";
    chk.Parameters.AddWithValue("@uid", uid);
    if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0) { Console.WriteLine($"  [skip] dados bancários {email}"); return; }

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO dados_bancarios
        (id, usuario_id, tipo_chave_pix, chave_pix, nome_completo, cpf_cnpj, nome_banco, criado_em, atualizado_em)
        VALUES (@id,@uid,@tipo,@chave,@nome,@cpf,'Nubank',@now,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@uid", uid);
    cmd.Parameters.AddWithValue("@tipo", tipoChave);
    cmd.Parameters.AddWithValue("@chave", chave);
    cmd.Parameters.AddWithValue("@nome", nome);
    cmd.Parameters.AddWithValue("@cpf", cpf);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [ok] {email}");
}

await UpsertDadosBancarios("mariana.limpeza@email.com",   "Cpf",      "123.456.789-01",    "Mariana Costa",  "12345678901");
await UpsertDadosBancarios("joao.encanador@email.com",    "Telefone", "+5521999887766",     "João Ferreira",  "98765432100");
await UpsertDadosBancarios("fernanda.pintura@email.com",  "Email",    "fernanda@email.com", "Fernanda Alves", "45678912300");
await UpsertDadosBancarios("carlos.eletricista@email.com","Cpf",      "111.222.333-44",     "Carlos Silva",   "11122233344");

// ─── Serviços — um por status do enum StatusServico ───────────────────────────
// Enum real: EmNegociacao, AguardandoPagamento, Pago, EmAndamento,
//            AguardandoConfirmacaoCliente, EmDisputa, Concluido, Cancelado
Console.WriteLine("\n▶ Criando serviços (cobrindo todos os StatusServico)...");
var sids = new Dictionary<string, string>(); // chave → id

async Task<string?> CriarServico(string chave, string titulo, string desc, string catParcial,
    string cidParcial, string clienteEmail, string? prestadorEmail,
    decimal preco, string status, string endereco, string? agendado = null,
    string? concluido = null, string? aguardandoDesde = null, string? criadoEm = null)
{
    if (!uids.TryGetValue(clienteEmail, out var cliId)) return null;

    await using var chk = conn.CreateCommand();
    chk.CommandText = "SELECT id FROM servicos WHERE titulo = @t AND cliente_id = @c";
    chk.Parameters.AddWithValue("@t", titulo);
    chk.Parameters.AddWithValue("@c", cliId);
    var existId = (await chk.ExecuteScalarAsync())?.ToString();
    if (existId != null) { Console.WriteLine($"  [skip] {titulo}"); sids[chave] = existId; return existId; }

    var preId = prestadorEmail != null && uids.TryGetValue(prestadorEmail, out var pid) ? pid : null;
    var id = NewId();
    var criado = criadoEm ?? Ago(5);

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO servicos
        (id, titulo, descricao, categoria_id, cidade_id, cliente_id, prestador_id, preco,
         taxa_admin_percentual, status, endereco, agendado_em, concluido_em,
         aguardando_confirmacao_desde, criado_em, atualizado_em)
        VALUES (@id,@t,@d,@cat,@cid,@cli,@pre,@preco,0.2000,@status,@end,@agend,@conc,@ag_desde,@criado,@now)";
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@t", titulo);
    cmd.Parameters.AddWithValue("@d", desc);
    cmd.Parameters.AddWithValue("@cat", GetCat(catParcial));
    cmd.Parameters.AddWithValue("@cid", GetCid(cidParcial));
    cmd.Parameters.AddWithValue("@cli", cliId);
    cmd.Parameters.AddWithValue("@pre", (object?)preId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@preco", preco);
    cmd.Parameters.AddWithValue("@status", status);
    cmd.Parameters.AddWithValue("@end", endereco);
    cmd.Parameters.AddWithValue("@agend", (object?)agendado ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@conc", (object?)concluido ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@ag_desde", (object?)aguardandoDesde ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@criado", criado);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
    sids[chave] = id;
    Console.WriteLine($"  [ok] [{status}] {titulo}");
    return id;
}

// 1. EmNegociacao — prestador ainda não foi selecionado, conversa em andamento
await CriarServico("limpeza-negociacao",   "Limpeza residencial completa",
    "Limpeza completa de apartamento de 70m², incluindo banheiros, cozinha e quartos.",
    "limpeza", "paulo", "ana.souza@email.com", "mariana.limpeza@email.com",
    180.00m, "EmNegociacao", "Rua das Flores, 123 - Vila Madalena, SP",
    agendado: Em(3), criadoEm: Ago(1));

// 2. AguardandoPagamento — serviço aprovado, cliente deve pagar via PIX
await CriarServico("pintura-aguardando-pgto", "Pintura de quarto e sala",
    "Pintura completa de quarto (15m²) e sala (25m²), duas demãos, cor branco gelo.",
    "pintura", "paulo", "ana.souza@email.com", "fernanda.pintura@email.com",
    800.00m, "AguardandoPagamento", "Rua Pamplona, 88 - Jardins, SP",
    agendado: Em(2), criadoEm: Ago(4));

// 3. Pago — pagamento recebido, serviço aguardando início
await CriarServico("eletrica-pago",       "Instalação de tomadas e interruptores",
    "Instalar 8 tomadas novas e 4 interruptores em apartamento reformado.",
    "eletrica", "janeiro", "bruno.lima@email.com", "carlos.eletricista@email.com",
    350.00m, "Pago", "Av. Atlântica, 500 - Copacabana, RJ",
    agendado: Em(1), criadoEm: Ago(2));

// 4. EmAndamento — prestador está executando o serviço
await CriarServico("encanamento-andamento","Conserto de vazamento na cozinha",
    "Vazamento embaixo da pia da cozinha, possível problema no sifão e vedação.",
    "encanamento", "horizonte", "carla.mendes@email.com", "joao.encanador@email.com",
    220.00m, "EmAndamento", "Rua da Bahia, 42 - Centro, BH",
    agendado: AgoH(2), criadoEm: Ago(1));

// 5. AguardandoConfirmacaoCliente — prestador marcou como concluído, cliente confirma
await CriarServico("pintura-aguard-confirm","Pintura de fachada do sobrado",
    "Pintura da fachada, área de aproximadamente 80m². Trabalho concluído pelo prestador.",
    "pintura", "paulo", "bruno.lima@email.com", "fernanda.pintura@email.com",
    1200.00m, "AguardandoConfirmacaoCliente", "Rua Augusta, 200 - Consolação, SP",
    agendado: Ago(2), criadoEm: Ago(8), aguardandoDesde: AgoH(6));

// 6. EmDisputa — cliente contestou a conclusão
await CriarServico("eletrica-disputa",    "Revisão elétrica completa do apartamento",
    "Revisão geral da instalação elétrica, 4 cômodos, verificação de disjuntores e tomadas.",
    "eletrica", "horizonte", "carla.mendes@email.com", "carlos.eletricista@email.com",
    650.00m, "EmDisputa", "Av. do Contorno, 500 - Savassi, BH",
    agendado: Ago(5), criadoEm: Ago(10), concluido: Ago(3));

// 7. Concluido — serviço finalizado e confirmado pelo cliente (gera cobrança)
await CriarServico("limpeza-concluido",   "Limpeza pós-obra",
    "Limpeza pesada após reforma, remoção de entulho, pó de cimento e resíduos de construção.",
    "limpeza", "janeiro", "bruno.lima@email.com", "mariana.limpeza@email.com",
    450.00m, "Concluido", "Rua Marquês de Abrantes, 12 - Flamengo, RJ",
    agendado: Ago(7), criadoEm: Ago(10), concluido: Ago(2));

// 7b. Concluido extra — para ter mais avaliações e cobranças
await CriarServico("encanamento-concluido","Instalação de aquecedor a gás",
    "Instalação completa de aquecedor a gás passagem com tubulação e teste de pressão.",
    "encanamento", "paulo", "ana.souza@email.com", "joao.encanador@email.com",
    380.00m, "Concluido", "Rua Haddock Lobo, 55 - Cerqueira César, SP",
    agendado: Ago(14), criadoEm: Ago(17), concluido: Ago(12));

// 8. Cancelado — serviço cancelado antes de iniciar
await CriarServico("eletrica-cancelado",  "Troca de tomadas e disjuntor",
    "Trocar 5 tomadas antigas e verificar disjuntor principal do apartamento.",
    "eletrica", "horizonte", "carla.mendes@email.com", "carlos.eletricista@email.com",
    180.00m, "Cancelado", "Av. do Contorno, 500 - Savassi, BH",
    criadoEm: Ago(15));

// ─── Cobranças — cobrindo todos os StatusCobranca ─────────────────────────────
// Enum real: Pendente, Pago, Retido, Liberado, Reembolsado, Cancelado
Console.WriteLine("\n▶ Criando cobranças (cobrindo todos os StatusCobranca)...");

async Task<string?> UpsertCobranca(string chaveServico, decimal valorTotal, decimal taxaAdmin, decimal valorPrestador,
    string status, string? pixQrCode = null, string? pixCopiaCola = null,
    string? pagarmeOrderId = null, string? pagarmePaymentId = null,
    string? pagoEm = null, string? retidoEm = null, string? liberadoEm = null,
    string? pixExpiraEm = null, string? criadoEm = null)
{
    if (!sids.TryGetValue(chaveServico, out var svcId)) return null;
    await using var chk = conn.CreateCommand();
    chk.CommandText = "SELECT id FROM cobrancas WHERE servico_id = @id";
    chk.Parameters.AddWithValue("@id", svcId);
    var existId = (await chk.ExecuteScalarAsync())?.ToString();
    if (existId != null) { Console.WriteLine($"  [skip] cobrança {chaveServico}"); return existId; }

    var id = NewId();
    var criado = criadoEm ?? Now();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO cobrancas
        (id, servico_id, valor_total, taxa_admin, valor_prestador, status,
         pix_qr_code, pix_copia_cola, pix_expira_em,
         pagarme_order_id, pagarme_payment_id,
         pago_em, retido_em, liberado_em, criado_em, atualizado_em)
        VALUES (@id,@svc,@vt,@ta,@vp,@status,@qr,@cc,@expira,@oid,@pid,@pago,@retido,@liberado,@criado,@now)";
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@svc", svcId);
    cmd.Parameters.AddWithValue("@vt", valorTotal);
    cmd.Parameters.AddWithValue("@ta", taxaAdmin);
    cmd.Parameters.AddWithValue("@vp", valorPrestador);
    cmd.Parameters.AddWithValue("@status", status);
    cmd.Parameters.AddWithValue("@qr", (object?)pixQrCode ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@cc", (object?)pixCopiaCola ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@expira", (object?)pixExpiraEm ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@oid", (object?)pagarmeOrderId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@pid", (object?)pagarmePaymentId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@pago", (object?)pagoEm ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@retido", (object?)retidoEm ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@liberado", (object?)liberadoEm ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@criado", criado);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [ok] [{status}] cobrança {chaveServico} R${valorTotal}");
    return id;
}

// Cobrança Pendente — PIX gerado, aguardando pagamento (serviço AguardandoPagamento)
var pixFicticioQr  = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
var pixFicticioCc  = "00020126580014BR.GOV.BCB.PIX0136a1b2c3d4-e5f6-7890-abcd-ef1234567890520400005303986540540.005802BR5913Prontto Ltda6009Sao Paulo62070503***63041234";
var pixExpira      = Em(1); // expira em 24h

await UpsertCobranca("pintura-aguardando-pgto", 800.00m, 160.00m, 640.00m,
    status: "Pendente",
    pixQrCode: pixFicticioQr,
    pixCopiaCola: pixFicticioCc,
    pagarmeOrderId: "or_ABCDEF1234567890",
    pagarmePaymentId: "py_ABCDEF1234567890",
    pixExpiraEm: pixExpira,
    criadoEm: Ago(1));

// Cobrança Pago — PIX confirmado, serviço validado (serviço Pago)
await UpsertCobranca("eletrica-pago", 350.00m, 70.00m, 280.00m,
    status: "Pago",
    pagarmeOrderId: "or_PAGO1234567890AB",
    pagarmePaymentId: "py_PAGO1234567890AB",
    pagoEm: Ago(1),
    criadoEm: Ago(2));

// Cobrança Retido — pago mas repasse bloqueado aguardando conclusão confirmada
await UpsertCobranca("limpeza-concluido", 450.00m, 90.00m, 360.00m,
    status: "Retido",
    pagarmeOrderId: "or_RETIDO123456789A",
    pagarmePaymentId: "py_RETIDO123456789A",
    pagoEm: Ago(3),
    retidoEm: Ago(2),
    criadoEm: Ago(3));

// Cobrança Liberado — repasse ao prestador executado
await UpsertCobranca("encanamento-concluido", 380.00m, 76.00m, 304.00m,
    status: "Liberado",
    pagarmeOrderId: "or_LIBERADO12345678",
    pagarmePaymentId: "py_LIBERADO12345678",
    pagoEm: Ago(13),
    retidoEm: Ago(12),
    liberadoEm: Ago(5),
    criadoEm: Ago(13));

// Cobrança Reembolsado — disputa resolvida a favor do cliente
await UpsertCobranca("eletrica-disputa", 650.00m, 130.00m, 520.00m,
    status: "Reembolsado",
    pagarmeOrderId: "or_REEMB12345678901",
    pagarmePaymentId: "py_REEMB12345678901",
    pagoEm: Ago(4),
    criadoEm: Ago(6));

// ─── Disputas ─────────────────────────────────────────────────────────────────
// Enum StatusDisputa: Aberta, EmAnalise, ResolvidaCliente, ResolvidaPrestador
Console.WriteLine("\n▶ Criando disputas...");

async Task UpsertDisputa(string chaveServico, string abreuPorEmail, string motivo,
    string descricao, string status)
{
    if (!sids.TryGetValue(chaveServico, out var svcId)) return;
    await using var chk = conn.CreateCommand();
    chk.CommandText = "SELECT COUNT(*) FROM disputas WHERE servico_id = @id";
    chk.Parameters.AddWithValue("@id", svcId);
    if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0)
    { Console.WriteLine($"  [skip] disputa {chaveServico}"); return; }

    if (!uids.TryGetValue(abreuPorEmail, out var abreuId)) return;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO disputas
        (id, servico_id, aberto_por_id, motivo, descricao, status, criado_em)
        VALUES (@id,@svc,@abreu,@motivo,@desc,@status,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@svc", svcId);
    cmd.Parameters.AddWithValue("@abreu", abreuId);
    cmd.Parameters.AddWithValue("@motivo", motivo);
    cmd.Parameters.AddWithValue("@desc", descricao);
    cmd.Parameters.AddWithValue("@status", status);
    cmd.Parameters.AddWithValue("@now", Ago(3));
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [ok] [{status}] disputa {chaveServico}");
}

// Disputa Aberta — cliente contestou recentemente
await UpsertDisputa("eletrica-disputa",
    abreuPorEmail: "carla.mendes@email.com",
    motivo: "Serviço não concluído conforme combinado",
    descricao: "O prestador marcou o serviço como concluído, mas 2 tomadas não foram instaladas e o disjuntor continua com problemas. O serviço foi parcialmente executado.",
    status: "Aberta");

// ─── Avaliações ────────────────────────────────────────────────────────────────
// Tabela usa nomes em inglês: service_id, reviewer_id, reviewed_id, rating, comment, created_at
// Unique constraint: (service_id, reviewer_id)
Console.WriteLine("\n▶ Criando avaliações...");

async Task<bool> AvaliacaoExiste(string chaveServico, string reviewerEmail)
{
    if (!sids.TryGetValue(chaveServico, out var sid)) return true;
    if (!uids.TryGetValue(reviewerEmail, out var reviewerId)) return true;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM avaliacoes WHERE service_id = @sid AND reviewer_id = @rid";
    cmd.Parameters.AddWithValue("@sid", sid);
    cmd.Parameters.AddWithValue("@rid", reviewerId);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
}

async Task InsertAvaliacao(string chaveServico, string reviewerEmail, string reviewedEmail,
    int rating, string? comment, string criadoEm)
{
    if (!sids.TryGetValue(chaveServico, out var sid)) return;
    if (!uids.TryGetValue(reviewerEmail, out var reviewerId)) return;
    if (!uids.TryGetValue(reviewedEmail, out var reviewedId)) return;

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO avaliacoes
        (id, service_id, reviewer_id, reviewed_id, rating, comment, created_at)
        VALUES (@id,@sid,@reviewer,@reviewed,@rating,@comment,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@sid", sid);
    cmd.Parameters.AddWithValue("@reviewer", reviewerId);
    cmd.Parameters.AddWithValue("@reviewed", reviewedId);
    cmd.Parameters.AddWithValue("@rating", rating);
    cmd.Parameters.AddWithValue("@comment", (object?)comment ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@now", criadoEm);
    await cmd.ExecuteNonQueryAsync();
}

// Serviço limpeza-concluido: avaliação bilateral
if (!await AvaliacaoExiste("limpeza-concluido", "bruno.lima@email.com"))
{
    await InsertAvaliacao("limpeza-concluido",
        reviewerEmail: "bruno.lima@email.com",
        reviewedEmail: "mariana.limpeza@email.com",
        rating: 5,
        comment: "Mariana é incrível! Apartamento ficou impecável após a reforma. Super pontual e profissional. Recomendo demais!",
        criadoEm: Ago(1));
    Console.WriteLine("  [ok] avaliação: Bruno → Mariana (limpeza-concluido) nota 5");
}
if (!await AvaliacaoExiste("limpeza-concluido", "mariana.limpeza@email.com"))
{
    await InsertAvaliacao("limpeza-concluido",
        reviewerEmail: "mariana.limpeza@email.com",
        reviewedEmail: "bruno.lima@email.com",
        rating: 5,
        comment: "Bruno foi um cliente excelente! Muito educado, comunicativo e pagou sem atrasos. Ótima experiência.",
        criadoEm: Ago(1));
    Console.WriteLine("  [ok] avaliação: Mariana → Bruno (limpeza-concluido) nota 5");
}

// Serviço encanamento-concluido: avaliação bilateral
if (!await AvaliacaoExiste("encanamento-concluido", "ana.souza@email.com"))
{
    await InsertAvaliacao("encanamento-concluido",
        reviewerEmail: "ana.souza@email.com",
        reviewedEmail: "joao.encanador@email.com",
        rating: 4,
        comment: "João fez um bom trabalho na instalação do aquecedor. Ficou bem acabado. Demorou um pouco mais que o previsto mas o resultado foi ótimo.",
        criadoEm: Ago(11));
    Console.WriteLine("  [ok] avaliação: Ana → João (encanamento-concluido) nota 4");
}
if (!await AvaliacaoExiste("encanamento-concluido", "joao.encanador@email.com"))
{
    await InsertAvaliacao("encanamento-concluido",
        reviewerEmail: "joao.encanador@email.com",
        reviewedEmail: "ana.souza@email.com",
        rating: 5,
        comment: "Ana foi uma ótima cliente! Casa organizada, me deixou trabalhar com conforto e pagou pontualmente.",
        criadoEm: Ago(11));
    Console.WriteLine("  [ok] avaliação: João → Ana (encanamento-concluido) nota 5");
}

// ─── Atualizar media_avaliacoes e total_avaliacoes nos prestadores ────────────
Console.WriteLine("\n▶ Atualizando médias de avaliações dos prestadores...");

async Task AtualizarMediaPrestador(string email)
{
    if (!uids.TryGetValue(email, out var uid)) return;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE usuarios SET
            media_avaliacoes = COALESCE((
                SELECT ROUND(AVG(rating), 2) FROM avaliacoes WHERE reviewed_id = @uid
            ), 0.00),
            total_avaliacoes = COALESCE((
                SELECT COUNT(*) FROM avaliacoes WHERE reviewed_id = @uid
            ), 0),
            atualizado_em = @now
        WHERE id = @uid";
    cmd.Parameters.AddWithValue("@uid", uid);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  [ok] média atualizada: {email}");
}

await AtualizarMediaPrestador("mariana.limpeza@email.com");
await AtualizarMediaPrestador("joao.encanador@email.com");
await AtualizarMediaPrestador("fernanda.pintura@email.com");
await AtualizarMediaPrestador("carlos.eletricista@email.com");

// ─── Portfólio de imagens ─────────────────────────────────────────────────────
// Colunas reais: id, usuario_id, url, cloudinary_public_id, moderado, aprovado,
//               ordem_exibicao, criado_em, deletado_em
Console.WriteLine("\n▶ Criando portfólio de imagens...");

async Task<bool> PortfolioExiste(string email)
{
    if (!uids.TryGetValue(email, out var uid)) return true;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM imagens_portfolio WHERE usuario_id = @uid";
    cmd.Parameters.AddWithValue("@uid", uid);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
}

async Task InsertImagem(string email, string urlSeed, int ordem)
{
    if (!uids.TryGetValue(email, out var uid)) return;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO imagens_portfolio
        (id, usuario_id, url, cloudinary_public_id, moderado, aprovado, ordem_exibicao, criado_em)
        VALUES (@id,@uid,@url,'',1,1,@ordem,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@uid", uid);
    cmd.Parameters.AddWithValue("@url", $"https://picsum.photos/seed/{urlSeed}/400/300");
    cmd.Parameters.AddWithValue("@ordem", ordem);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
}

if (!await PortfolioExiste("mariana.limpeza@email.com"))
{
    await InsertImagem("mariana.limpeza@email.com", "limpeza1", 0);
    await InsertImagem("mariana.limpeza@email.com", "limpeza2", 1);
    await InsertImagem("mariana.limpeza@email.com", "limpeza3", 2);
    Console.WriteLine("  [ok] portfólio: Mariana (3 imagens)");
}
if (!await PortfolioExiste("joao.encanador@email.com"))
{
    await InsertImagem("joao.encanador@email.com", "encanamento1", 0);
    await InsertImagem("joao.encanador@email.com", "encanamento2", 1);
    await InsertImagem("joao.encanador@email.com", "encanamento3", 2);
    Console.WriteLine("  [ok] portfólio: João (3 imagens)");
}
if (!await PortfolioExiste("fernanda.pintura@email.com"))
{
    await InsertImagem("fernanda.pintura@email.com", "pintura1", 0);
    await InsertImagem("fernanda.pintura@email.com", "pintura2", 1);
    Console.WriteLine("  [ok] portfólio: Fernanda (2 imagens)");
}
if (!await PortfolioExiste("carlos.eletricista@email.com"))
{
    await InsertImagem("carlos.eletricista@email.com", "eletrica1", 0);
    await InsertImagem("carlos.eletricista@email.com", "eletrica2", 1);
    await InsertImagem("carlos.eletricista@email.com", "eletrica3", 2);
    Console.WriteLine("  [ok] portfólio: Carlos (3 imagens)");
}

// ─── Mensagens de chat ────────────────────────────────────────────────────────
Console.WriteLine("\n▶ Criando mensagens de chat...");

async Task<bool> MensagensExistem(string chave)
{
    if (!sids.TryGetValue(chave, out var sid)) return true;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM mensagens_servico WHERE servico_id = @id";
    cmd.Parameters.AddWithValue("@id", sid);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
}

async Task Msg(string chave, string email, string papel, string conteudo, int minutosAtras = 0)
{
    if (!sids.TryGetValue(chave, out var sid)) return;
    uids.TryGetValue(email, out var remId);
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO mensagens_servico
        (id, servico_id, remetente_id, papel_remetente, tipo_mensagem, conteudo, imagem_moderada, criado_em)
        VALUES (@id,@sid,@rem,@papel,'Texto',@cont,0,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@sid", sid);
    cmd.Parameters.AddWithValue("@rem", (object?)remId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@papel", papel);
    cmd.Parameters.AddWithValue("@cont", conteudo);
    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.AddMinutes(-minutosAtras).ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
    await cmd.ExecuteNonQueryAsync();
}

// EmNegociacao — limpeza-negociacao
if (!await MensagensExistem("limpeza-negociacao"))
{
    await Msg("limpeza-negociacao", "ana.souza@email.com",        "Cliente",   "Olá Mariana! Vi seu perfil e achei ótimo. Preciso de limpeza completa do apartamento para o próximo sábado.", 90);
    await Msg("limpeza-negociacao", "mariana.limpeza@email.com",  "Prestador", "Oi Ana! Fico feliz! O sábado está disponível sim. O apartamento tem quantos cômodos?", 75);
    await Msg("limpeza-negociacao", "ana.souza@email.com",        "Cliente",   "São 2 quartos, 2 banheiros, sala, cozinha e área de serviço. Uns 70m² no total.", 60);
    await Msg("limpeza-negociacao", "mariana.limpeza@email.com",  "Prestador", "Perfeito! O valor de R$180 cobre tudo isso. Inclui todos os produtos de limpeza.", 45);
    await Msg("limpeza-negociacao", "ana.souza@email.com",        "Cliente",   "Ótimo! Pode usar produtos ecológicos? Tenho um filho pequeno.", 30);
    await Msg("limpeza-negociacao", "mariana.limpeza@email.com",  "Prestador", "Claro! Trabalho com linha Baby Safe, totalmente segura para crianças e pets. Confirme o pedido para eu bloquear a agenda.", 15);
    Console.WriteLine("  [ok] Mensagens: EmNegociacao — limpeza (6)");
}

// AguardandoPagamento — pintura-aguardando-pgto
if (!await MensagensExistem("pintura-aguardando-pgto"))
{
    await Msg("pintura-aguardando-pgto", "ana.souza@email.com",         "Cliente",   "Fernanda, qual tinta você está usando? Quero branco gelo mesmo, bem clarinho.", 5760);
    await Msg("pintura-aguardando-pgto", "fernanda.pintura@email.com",  "Prestador", "Estou usando Suvinil Premium Ana, é a melhor para interior. Cobre muito bem em duas demãos.", 5740);
    await Msg("pintura-aguardando-pgto", "ana.souza@email.com",         "Cliente",   "Perfeito! Pode confirmar o serviço então? Quero garantir a data.", 5720);
    await Msg("pintura-aguardando-pgto", "fernanda.pintura@email.com",  "Prestador", "Confirmado! Assim que o pagamento for processado, a data estará garantida na minha agenda.", 5700);
    await Msg("pintura-aguardando-pgto", "ana.souza@email.com",         "Cliente",   "Ótimo! Vou efetuar o pagamento agora pelo PIX.", 30);
    Console.WriteLine("  [ok] Mensagens: AguardandoPagamento — pintura (5)");
}

// Pago — eletrica-pago
if (!await MensagensExistem("eletrica-pago"))
{
    await Msg("eletrica-pago", "bruno.lima@email.com",          "Cliente",   "Olá Carlos! Preciso instalar tomadas novas no apartamento reformado. São 8 tomadas e 4 interruptores.", 2880);
    await Msg("eletrica-pago", "carlos.eletricista@email.com",  "Prestador", "Oi Bruno! Consigo fazer sim. O valor de R$350 inclui material e mão de obra. Tudo novo, fio de cobre 2,5mm.", 2860);
    await Msg("eletrica-pago", "bruno.lima@email.com",          "Cliente",   "Perfeito! Pode confirmar para amanhã às 9h? Estarei em casa.", 2840);
    await Msg("eletrica-pago", "carlos.eletricista@email.com",  "Prestador", "Confirmado! Estarei lá às 9h em ponto. Qualquer coisa me chama.", 2820);
    await Msg("eletrica-pago", "bruno.lima@email.com",          "Cliente",   "Pagamento realizado! Fico aguardando amanhã.", 120);
    await Msg("eletrica-pago", "carlos.eletricista@email.com",  "Prestador", "Recebi a confirmação! Até amanhã Bruno.", 100);
    Console.WriteLine("  [ok] Mensagens: Pago — elétrica (6)");
}

// EmAndamento — encanamento-andamento
if (!await MensagensExistem("encanamento-andamento"))
{
    await Msg("encanamento-andamento", "carla.mendes@email.com",      "Cliente",   "João, o vazamento está piorando! Está molhando todo o armário embaixo da pia.", 180);
    await Msg("encanamento-andamento", "joao.encanador@email.com",    "Prestador", "Entendi Carla, pode ser o sifão ou a vedação do cano. Já estou saindo, chego em 20 minutos.", 160);
    await Msg("encanamento-andamento", "joao.encanador@email.com",    "Prestador", "Cheguei! Confirmei que é o sifão mesmo, trincado. Vou trocar agora, tenho a peça na van.", 90);
    await Msg("encanamento-andamento", "carla.mendes@email.com",      "Cliente",   "Ufa! Obrigada pela agilidade João. Fiquei aliviada.", 85);
    await Msg("encanamento-andamento", "joao.encanador@email.com",    "Prestador", "Sifão trocado e testado. Vou verificar também se há outros pontos de vazamento.", 60);
    await Msg("encanamento-andamento", "carla.mendes@email.com",      "Cliente",   "Por favor João, pode verificar também a torneira da pia que está pingando.", 45);
    await Msg("encanamento-andamento", "joao.encanador@email.com",    "Prestador", "Verificando agora. A torneira precisa de um reparo no vedante, conserto incluso no valor.", 30);
    Console.WriteLine("  [ok] Mensagens: EmAndamento — encanamento (7)");
}

// AguardandoConfirmacaoCliente — pintura-aguard-confirm
if (!await MensagensExistem("pintura-aguard-confirm"))
{
    await Msg("pintura-aguard-confirm", "bruno.lima@email.com",         "Cliente",   "Fernanda, como está o andamento da pintura da fachada?", 14400);
    await Msg("pintura-aguard-confirm", "fernanda.pintura@email.com",   "Prestador", "Bruno, está indo ótimo! Primeira demão feita. Amanhã termino.", 14380);
    await Msg("pintura-aguard-confirm", "fernanda.pintura@email.com",   "Prestador", "Bruno, terminei! A fachada está linda! Marquei o serviço como concluído. Por favor confirme quando puder inspecionar.", 360);
    await Msg("pintura-aguard-confirm", "bruno.lima@email.com",         "Cliente",   "Vou passar no final do dia para conferir. Obrigado pelo aviso!", 350);
    await Msg("pintura-aguard-confirm", "bruno.lima@email.com",         "Cliente",   "Fernanda, ficou muito bonito! Vou confirmar a conclusão agora.", 60);
    Console.WriteLine("  [ok] Mensagens: AguardandoConfirmacaoCliente — pintura fachada (5)");
}

// EmDisputa — eletrica-disputa
if (!await MensagensExistem("eletrica-disputa"))
{
    await Msg("eletrica-disputa", "carla.mendes@email.com",      "Cliente",   "Carlos, preciso que você revise toda a instalação elétrica. Estamos reformando o apartamento.", 14400);
    await Msg("eletrica-disputa", "carlos.eletricista@email.com","Prestador", "Carla, pode deixar! Farei a revisão completa. Qual o melhor dia?", 14380);
    await Msg("eletrica-disputa", "carla.mendes@email.com",      "Cliente",   "Pode ser quinta-feira às 14h.", 14360);
    await Msg("eletrica-disputa", "carlos.eletricista@email.com","Prestador", "Perfeito! Estarei lá.", 14340);
    await Msg("eletrica-disputa", "carlos.eletricista@email.com","Prestador", "Carla, marquei o serviço como concluído.", 4320);
    await Msg("eletrica-disputa", "carla.mendes@email.com",      "Cliente",   "Carlos, 2 tomadas não foram instaladas e o disjuntor continua com problema! Abri uma disputa.", 4300);
    await Msg("eletrica-disputa", "carlos.eletricista@email.com","Prestador", "Carla, as tomadas que mencionou não estavam no escopo inicial combinado.", 4280);
    await Msg("eletrica-disputa", "carla.mendes@email.com",      "Cliente",   "Estavam sim! Está descrito no pedido. O admin vai avaliar.", 4260);
    Console.WriteLine("  [ok] Mensagens: EmDisputa — elétrica (8)");
}

// Concluido — limpeza-concluido
if (!await MensagensExistem("limpeza-concluido"))
{
    await Msg("limpeza-concluido", "bruno.lima@email.com",         "Cliente",   "Mariana, a reforma terminou mas ficou um caos. Tem cimento, tinta, poeira e entulho.", 17280);
    await Msg("limpeza-concluido", "mariana.limpeza@email.com",    "Prestador", "Entendo Bruno! Pós-obra é pesado mesmo. O valor de R$450 já inclui todos os produtos específicos.", 17260);
    await Msg("limpeza-concluido", "bruno.lima@email.com",         "Cliente",   "Perfeito! Pode começar na segunda-feira às 8h?", 17240);
    await Msg("limpeza-concluido", "mariana.limpeza@email.com",    "Prestador", "Pode deixar! Estarei lá com minha equipe. Estimativa de 6 horas de trabalho.", 17220);
    await Msg("limpeza-concluido", "mariana.limpeza@email.com",    "Prestador", "Cheguei! Começando o trabalho agora.", 11520);
    await Msg("limpeza-concluido", "mariana.limpeza@email.com",    "Prestador", "Trabalho finalizado! O apartamento está impecável. Tirei fotos para o portfolio se você permitir.", 5760);
    await Msg("limpeza-concluido", "bruno.lima@email.com",         "Cliente",   "Perfeito Mariana! Ficou incrível, muito obrigado! Pode usar as fotos sim.", 5740);
    await Msg("limpeza-concluido", "bruno.lima@email.com",         "Cliente",   "Confirmei a conclusão. Obrigado pelo excelente trabalho!", 2880);
    Console.WriteLine("  [ok] Mensagens: Concluido — limpeza pós-obra (8)");
}

// Concluido extra — encanamento-concluido
if (!await MensagensExistem("encanamento-concluido"))
{
    await Msg("encanamento-concluido", "ana.souza@email.com",        "Cliente",   "João, preciso instalar um aquecedor a gás. Você faz esse tipo de serviço?", 24480);
    await Msg("encanamento-concluido", "joao.encanador@email.com",   "Prestador", "Faço sim Ana! Instalação completa com tubulação e teste. R$380 tudo incluído.", 24460);
    await Msg("encanamento-concluido", "ana.souza@email.com",        "Cliente",   "Perfeito! Pode ser semana que vem?", 24440);
    await Msg("encanamento-concluido", "joao.encanador@email.com",   "Prestador", "Pode! Confirme o pedido e agendamos.", 24420);
    await Msg("encanamento-concluido", "joao.encanador@email.com",   "Prestador", "Ana, instalação concluída! Aquecedor funcionando perfeitamente, testei a pressão.", 20160);
    await Msg("encanamento-concluido", "ana.souza@email.com",        "Cliente",   "Que ótimo João! Muito obrigada, ficou excelente!", 20140);
    Console.WriteLine("  [ok] Mensagens: Concluido — encanamento (6)");
}

// ─── Notificações ─────────────────────────────────────────────────────────────
Console.WriteLine("\n▶ Criando notificações...");

async Task<bool> NotifExiste(string email)
{
    if (!uids.TryGetValue(email, out var uid)) return true;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM notificacoes WHERE usuario_id = @uid";
    cmd.Parameters.AddWithValue("@uid", uid);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
}

async Task Notif(string email, string titulo, string mensagem, string tipo, bool lido = false, string? referenciaId = null)
{
    if (!uids.TryGetValue(email, out var uid)) return;
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO notificacoes
        (id, usuario_id, titulo, mensagem, lido, tipo, referencia_id, criado_em)
        VALUES (@id,@uid,@titulo,@msg,@lido,@tipo,@ref,@now)";
    cmd.Parameters.AddWithValue("@id", NewId());
    cmd.Parameters.AddWithValue("@uid", uid);
    cmd.Parameters.AddWithValue("@titulo", titulo);
    cmd.Parameters.AddWithValue("@msg", mensagem);
    cmd.Parameters.AddWithValue("@lido", lido ? 1 : 0);
    cmd.Parameters.AddWithValue("@tipo", tipo);
    cmd.Parameters.AddWithValue("@ref", (object?)referenciaId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@now", Now());
    await cmd.ExecuteNonQueryAsync();
}

if (!await NotifExiste("ana.souza@email.com"))
{
    await Notif("ana.souza@email.com", "Proposta de serviço aceita", "Fernanda Alves aceitou seu pedido de pintura. Efetue o pagamento para garantir a data.", "Servico", referenciaId: sids.GetValueOrDefault("pintura-aguardando-pgto"));
    await Notif("ana.souza@email.com", "Novo PIX gerado", "QR Code PIX para pagamento da pintura de quarto e sala está pronto. Valor: R$800,00.", "Cobranca");
    await Notif("ana.souza@email.com", "Mariana respondeu sua mensagem", "Mariana Costa respondeu sobre a limpeza residencial.", "Mensagem", lido: true);
    Console.WriteLine("  [ok] Notificações Ana (3)");
}
if (!await NotifExiste("bruno.lima@email.com"))
{
    await Notif("bruno.lima@email.com", "Cobrança retida aguardando repasse", "Pagamento de R$450,00 pelo serviço de limpeza pós-obra foi recebido. Repasse em processamento.", "Cobranca", lido: true);
    await Notif("bruno.lima@email.com", "Serviço aguardando sua confirmação", "Fernanda Alves concluiu a pintura da fachada. Confirme para liberar o pagamento.", "Servico", referenciaId: sids.GetValueOrDefault("pintura-aguard-confirm"));
    await Notif("bruno.lima@email.com", "Carlos confirmou presença", "Carlos Silva estará no seu apartamento amanhã às 9h para a instalação elétrica.", "Servico", lido: true);
    Console.WriteLine("  [ok] Notificações Bruno (3)");
}
if (!await NotifExiste("carla.mendes@email.com"))
{
    await Notif("carla.mendes@email.com", "Disputa aberta com sucesso", "Sua disputa sobre a revisão elétrica foi registrada. O time Prontto irá analisar em até 48h.", "Servico", referenciaId: sids.GetValueOrDefault("eletrica-disputa"));
    await Notif("carla.mendes@email.com", "João está a caminho", "João Ferreira está a caminho para o conserto de vazamento. Previsão: 20 minutos.", "Servico", lido: true);
    await Notif("carla.mendes@email.com", "Reembolso em processamento", "Devido à disputa, o reembolso de R$650,00 está sendo processado.", "Cobranca");
    Console.WriteLine("  [ok] Notificações Carla (3)");
}
if (!await NotifExiste("mariana.limpeza@email.com"))
{
    await Notif("mariana.limpeza@email.com", "Novo pedido de serviço", "Ana Souza solicitou limpeza residencial completa para daqui 3 dias. Valor: R$180.", "Servico");
    await Notif("mariana.limpeza@email.com", "Repasse liberado!", "O valor de R$360,00 pelo serviço de limpeza pós-obra foi liberado para sua conta.", "Cobranca", lido: true);
    await Notif("mariana.limpeza@email.com", "Avaliação recebida", "Bruno Lima deixou uma avaliação 5 estrelas para o serviço de limpeza pós-obra.", "Servico", lido: true);
    Console.WriteLine("  [ok] Notificações Mariana (3)");
}
if (!await NotifExiste("carlos.eletricista@email.com"))
{
    await Notif("carlos.eletricista@email.com", "Disputa aberta pelo cliente", "Carla Mendes abriu uma disputa sobre a revisão elétrica. Aguarde análise do admin.", "Servico");
    await Notif("carlos.eletricista@email.com", "Pagamento confirmado", "Bruno Lima confirmou o pagamento de R$350,00 para a instalação elétrica. Compareça amanhã às 9h.", "Cobranca", lido: true);
    Console.WriteLine("  [ok] Notificações Carlos (2)");
}
if (!await NotifExiste("joao.encanador@email.com"))
{
    await Notif("joao.encanador@email.com", "Serviço em andamento", "Conserto de vazamento de Carla Mendes em andamento. Finalize quando concluir.", "Servico", lido: true);
    await Notif("joao.encanador@email.com", "Avaliação recebida", "Ana Souza deixou uma avaliação 4 estrelas pelo serviço de instalação de aquecedor.", "Servico", lido: true);
    await Notif("joao.encanador@email.com", "Repasse de R$304,00 liberado", "O repasse pelo serviço de instalação de aquecedor a gás foi enviado para sua chave PIX.", "Cobranca");
    Console.WriteLine("  [ok] Notificações João (3)");
}
if (!await NotifExiste("fernanda.pintura@email.com"))
{
    await Notif("fernanda.pintura@email.com", "Aguardando confirmação do cliente", "Bruno Lima ainda não confirmou a conclusão da pintura. Aguarde ou entre em contato.", "Servico");
    await Notif("fernanda.pintura@email.com", "Pagamento em processamento", "Ana Souza está realizando o pagamento de R$800,00 para pintura de quarto e sala.", "Cobranca");
    Console.WriteLine("  [ok] Notificações Fernanda (2)");
}
if (!await NotifExiste("admin@prontto.org"))
{
    await Notif("admin@prontto.org", "Disputa aberta — ação necessária", "Carla Mendes abriu disputa sobre revisão elétrica de Carlos Silva. Valor em jogo: R$650,00.", "Servico");
    await Notif("admin@prontto.org", "Novo prestador cadastrado", "Carlos Silva (carlos.eletricista@email.com) se cadastrou como prestador.", "Sistema", lido: true);
    await Notif("admin@prontto.org", "Repasse automático executado", "Repasse de R$304,00 para João Ferreira concluído com sucesso.", "Cobranca", lido: true);
    Console.WriteLine("  [ok] Notificações Admin (3)");
}

// ─── Resumo ───────────────────────────────────────────────────────────────────
Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════════════╗
║                      Seed concluído!                                     ║
╠══════════════════════════════════════════════════════════════════════════╣
║  Senha dos usuários de teste:  Senha123                                  ║
║  Senha do admin:               Admin123                                  ║
╠══════════════════════════════════════════════════════════════════════════╣
║  CLIENTES                                                                ║
║    ana.souza@email.com                                                   ║
║    bruno.lima@email.com                                                  ║
║    carla.mendes@email.com                                                ║
╠══════════════════════════════════════════════════════════════════════════╣
║  PRESTADORES                                                             ║
║    mariana.limpeza@email.com    → Limpeza                                ║
║    joao.encanador@email.com     → Encanamento                            ║
║    fernanda.pintura@email.com   → Pintura                                ║
║    carlos.eletricista@email.com → Elétrica                               ║
╠══════════════════════════════════════════════════════════════════════════╣
║  ADMIN                                                                   ║
║    admin@prontto.org  (senha: Admin123)                                  ║
╠══════════════════════════════════════════════════════════════════════════╣
║  SERVICOS — todos os StatusServico cobertos                              ║
║    EmNegociacao              Limpeza residencial completa                ║
║    AguardandoPagamento       Pintura de quarto e sala                    ║
║    Pago                      Instalação de tomadas e interruptores       ║
║    EmAndamento               Conserto de vazamento na cozinha            ║
║    AguardandoConfirmacao...  Pintura de fachada do sobrado               ║
║    EmDisputa                 Revisão elétrica + disputa aberta           ║
║    Concluido (x2)            Limpeza pós-obra + Aquecedor a gás         ║
║    Cancelado                 Troca de tomadas e disjuntor                ║
╠══════════════════════════════════════════════════════════════════════════╣
║  COBRANCAS — todos os StatusCobranca cobertos                            ║
║    Pendente     pintura-aguardando-pgto  R$800 (PIX fictício)            ║
║    Pago         eletrica-pago            R$350 (pagamento confirmado)    ║
║    Retido       limpeza-concluido        R$450 (aguardando repasse)      ║
║    Liberado     encanamento-concluido    R$380 (repasse executado)       ║
║    Reembolsado  eletrica-disputa         R$650 (disputa resolvida)       ║
╠══════════════════════════════════════════════════════════════════════════╣
║  AVALIACOES (bilateral)                                                  ║
║    Bruno  → Mariana  (limpeza-concluido)       nota 5                   ║
║    Mariana → Bruno   (limpeza-concluido)       nota 5                   ║
║    Ana    → João     (encanamento-concluido)   nota 4                   ║
║    João   → Ana      (encanamento-concluido)   nota 5                   ║
╠══════════════════════════════════════════════════════════════════════════╣
║  DISPUTAS                                                                ║
║    eletrica-disputa  status: Aberta  (aberta por Carla)                  ║
╠══════════════════════════════════════════════════════════════════════════╣
║  PORTFOLIO  Mariana (3) · João (3) · Fernanda (2) · Carlos (3)          ║
╠══════════════════════════════════════════════════════════════════════════╣
║  MENSAGENS  todos os serviços ativos têm chat com 5-8 mensagens          ║
╚══════════════════════════════════════════════════════════════════════════╝");
