using System.Net;

namespace Prontto.Application.Common;

/// <summary>
/// Modelos de e-mail transacional da Prontto, com cabeçalho e rodapé laranja da marca.
/// Layout em tabela + estilos inline para máxima compatibilidade com clientes de e-mail.
/// </summary>
public static class ModelosEmail
{
    private const string Laranja = "#f97316";

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    /// <summary>Envolve o conteúdo no layout da marca (header/footer laranja).</summary>
    public static string Layout(string conteudoHtml) => $@"<!doctype html>
<html><body style=""margin:0;background:#f4f5f7;padding:24px;font-family:Arial,Helvetica,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""><tr><td align=""center"">
<table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 6px 24px rgba(15,23,42,.08);"">
  <tr><td style=""background:{Laranja};padding:22px 28px;text-align:center;"">
    <span style=""color:#ffffff;font-size:24px;font-weight:800;letter-spacing:.5px;"">PRONTTO</span>
  </td></tr>
  <tr><td style=""padding:30px 28px;color:#0f172a;font-size:15px;line-height:1.65;"">{conteudoHtml}</td></tr>
  <tr><td style=""background:{Laranja};padding:16px 28px;text-align:center;color:#ffffff;font-size:12px;line-height:1.5;"">
    Prontto — serviços de confiança na sua região.<br>Este é um e-mail automático, não é necessário responder.
  </td></tr>
</table></td></tr></table></body></html>";

    /// <summary>Botão CTA laranja.</summary>
    public static string Botao(string texto, string url) => $@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:22px 0;""><tr>
<td style=""border-radius:9999px;background:{Laranja};"">
<a href=""{url}"" style=""display:inline-block;padding:13px 28px;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;border-radius:9999px;"">{Esc(texto)}</a>
</td></tr></table>";

    // ── 1) Boas-vindas ──────────────────────────────────────────────────────────

    public static string BoasVindasCliente(string nome, string appUrl) => Layout($@"
<h1 style=""margin:0 0 12px;font-size:20px;color:#0f172a;"">Bem-vindo(a) à Prontto, {Esc(nome)}! 🎉</h1>
<p style=""margin:0 0 10px;"">Sua conta de <b>contratante</b> está pronta. Encontre profissionais verificados na sua região e peça orçamento sem compromisso.</p>
<p style=""margin:0 0 4px;color:#64748b;"">Como funciona:</p>
<ul style=""margin:0 0 8px;padding-left:20px;color:#334155;"">
<li>Pesquise o serviço que você precisa</li>
<li>Compare profissionais e avaliações</li>
<li>Negocie e pague com segurança pelo app</li></ul>
{Botao("Buscar profissionais", appUrl + "/buscar")}
<p style=""margin:0;color:#64748b;font-size:13px;"">Qualquer dúvida, é só acessar a Central de Ajuda.</p>");

    public static string BoasVindasPrestador(string nome, string appUrl) => Layout($@"
<h1 style=""margin:0 0 12px;font-size:20px;color:#0f172a;"">Bem-vindo(a) à Prontto, {Esc(nome)}! 🛠️</h1>
<p style=""margin:0 0 10px;"">Sua conta de <b>profissional</b> foi criada. Complete seu perfil para começar a receber solicitações de serviço na sua região.</p>
<p style=""margin:0 0 4px;color:#64748b;"">Para receber mais clientes:</p>
<ul style=""margin:0 0 8px;padding-left:20px;color:#334155;"">
<li>Preencha sua descrição e especialidade</li>
<li>Selecione suas categorias e cidades de atuação</li>
<li>Adicione fotos do seu portfólio</li></ul>
{Botao("Completar meu perfil", appUrl + "/minha-area")}
<p style=""margin:0;color:#64748b;font-size:13px;"">Quanto mais completo o perfil, maiores as chances de ser contratado.</p>");

    // ── 2) Prestador recebeu mensagem/proposta ─────────────────────────────────

    public static string NovaMensagemPrestador(string nome, string servicoTitulo, bool ehProposta, decimal? valor, string url) => Layout($@"
<h1 style=""margin:0 0 12px;font-size:20px;color:#0f172a;"">{(ehProposta ? "Você recebeu uma proposta 💰" : "Você recebeu uma nova mensagem 💬")}</h1>
<p style=""margin:0 0 10px;"">Olá {Esc(nome)}, há novidade no serviço <b>{Esc(servicoTitulo)}</b>.</p>
{(ehProposta && valor.HasValue ? $@"<p style=""margin:0 0 10px;font-size:16px;"">Valor proposto: <b>R$ {valor.Value:N2}</b></p>" : "")}
{Botao(ehProposta ? "Acessar a proposta" : "Ver a mensagem", url)}
<p style=""margin:0;color:#64748b;font-size:13px;"">Acesse para responder, negociar ou aceitar.</p>");

    // ── 3) Contratante recebeu retorno da solicitação ──────────────────────────

    public static string RetornoSolicitacao(string nome, string servicoTitulo, bool ehProposta, decimal? valor, string url) => Layout($@"
<h1 style=""margin:0 0 12px;font-size:20px;color:#0f172a;"">Sua solicitação teve um retorno! 🔔</h1>
<p style=""margin:0 0 10px;"">Olá {Esc(nome)}, o profissional respondeu à sua solicitação de <b>{Esc(servicoTitulo)}</b>.</p>
{(ehProposta && valor.HasValue ? $@"<p style=""margin:0 0 10px;font-size:16px;"">Proposta recebida: <b>R$ {valor.Value:N2}</b></p>" : "")}
{Botao(ehProposta ? "Ver a proposta" : "Ver a resposta", url)}
<p style=""margin:0;color:#64748b;font-size:13px;"">Você pode negociar e, quando estiver de acordo, efetuar o pagamento com segurança.</p>");
}
