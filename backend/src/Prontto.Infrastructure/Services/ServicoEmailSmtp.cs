using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Envio de e-mail via SMTP (ex.: e-mail profissional Hostinger). Configurado por variáveis:
/// SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, SMTP_FROM, ADMIN_EMAIL.
/// Se o SMTP não estiver configurado, apenas registra em log e retorna false — sem quebrar o fluxo.
/// </summary>
public class ServicoEmailSmtp(IConfiguration configuracao, ILogger<ServicoEmailSmtp> logger) : IServicoEmail
{
    public Task<bool> EnviarParaAdminAsync(string assunto, string corpoHtml, string? responderPara = null)
    {
        var admin = configuracao["ADMIN_EMAIL"] ?? configuracao["SMTP_USER"];
        return EnviarInternoAsync(admin, "Administração Prontto", assunto, corpoHtml, responderPara);
    }

    public Task<bool> EnviarAsync(string destinatario, string? nomeDestinatario, string assunto, string corpoHtml)
        => EnviarInternoAsync(destinatario, nomeDestinatario, assunto, corpoHtml, null);

    private async Task<bool> EnviarInternoAsync(string? destinatario, string? nomeDestinatario, string assunto, string corpoHtml, string? responderPara)
    {
        var host = configuracao["SMTP_HOST"];
        var portaTxt = configuracao["SMTP_PORT"];
        var usuario = configuracao["SMTP_USER"];
        var senha = configuracao["SMTP_PASS"];
        var de = configuracao["SMTP_FROM"] ?? usuario;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(usuario)
            || string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(de)
            || string.IsNullOrWhiteSpace(destinatario))
        {
            logger.LogWarning("SMTP não configurado (ou destinatário vazio) — e-mail '{Assunto}' não enviado.", assunto);
            return false;
        }

        var porta = int.TryParse(portaTxt, out var p) ? p : 587;

        try
        {
            using var mensagem = new MailMessage
            {
                From = new MailAddress(de, "Prontto"),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true,
            };
            mensagem.To.Add(string.IsNullOrWhiteSpace(nomeDestinatario)
                ? new MailAddress(destinatario)
                : new MailAddress(destinatario, nomeDestinatario));
            if (!string.IsNullOrWhiteSpace(responderPara))
                mensagem.ReplyToList.Add(new MailAddress(responderPara));

            using var cliente = new SmtpClient(host, porta)
            {
                EnableSsl = porta != 25,
                Credentials = new NetworkCredential(usuario, senha),
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            await cliente.SendMailAsync(mensagem);
            logger.LogInformation("E-mail '{Assunto}' enviado para {Destinatario}.", assunto, destinatario);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar e-mail '{Assunto}' via SMTP {Host}:{Porta}.", assunto, host, porta);
            return false;
        }
    }
}
