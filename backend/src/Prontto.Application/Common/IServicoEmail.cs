namespace Prontto.Application.Common;

/// <summary>Envio de e-mail transacional (ex.: sugestões de serviço para o admin).</summary>
public interface IServicoEmail
{
    /// <summary>
    /// Envia um e-mail para o admin da Prontto. Retorna true se enviado; false se o SMTP
    /// não estiver configurado ou o envio falhar (nunca lança — o fluxo não deve quebrar).
    /// </summary>
    Task<bool> EnviarParaAdminAsync(string assunto, string corpoHtml, string? responderPara = null);
}
