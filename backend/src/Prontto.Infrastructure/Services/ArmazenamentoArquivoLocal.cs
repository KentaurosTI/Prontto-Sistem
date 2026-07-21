using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prontto.Application.Common;
using Prontto.Domain.Interfaces;

namespace Prontto.Infrastructure.Services;

/// <summary>
/// Implementação de armazenamento de arquivos no disco local do servidor.
/// Arquivos são salvos em uploads/{ano}/{mes}/{guid}.{extensao} e servidos
/// como arquivos estáticos via o middleware UseStaticFiles configurado no Program.cs.
/// </summary>
public class ArmazenamentoArquivoLocal(
    IHostEnvironment env,
    ILogger<ArmazenamentoArquivoLocal> logger) : IArmazenamentoArquivo
{
    private static readonly HashSet<string> _extensoesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private string PastaBase => Path.Combine(env.ContentRootPath, "uploads");

    public async Task<string> SalvarAsync(Stream conteudo, string nomeOriginal, string contentType)
    {
        var extensao = Path.GetExtension(nomeOriginal);
        if (string.IsNullOrWhiteSpace(extensao) || !_extensoesPermitidas.Contains(extensao))
            throw new ExcecaoValidacao(
                $"Tipo de arquivo não permitido. Use: {string.Join(", ", _extensoesPermitidas)}");

        var agora = DateTime.UtcNow;
        var subpasta = Path.Combine(agora.Year.ToString(), agora.Month.ToString("D2"));
        var pastaDestino = Path.Combine(PastaBase, subpasta);

        Directory.CreateDirectory(pastaDestino);

        var nomeArquivo = Guid.NewGuid().ToString("N") + extensao.ToLowerInvariant();
        var caminhoFisico = Path.Combine(pastaDestino, nomeArquivo);

        await using (var fluxoDestino = new FileStream(caminhoFisico, FileMode.Create, FileAccess.Write))
        {
            await conteudo.CopyToAsync(fluxoDestino);
        }

        // URL relativa acessível via middleware de arquivos estáticos
        var urlRelativa = $"/uploads/{agora.Year}/{agora.Month:D2}/{nomeArquivo}";

        logger.LogInformation("Arquivo salvo: {CaminhoFisico} → {UrlRelativa}", caminhoFisico, urlRelativa);

        return urlRelativa;
    }

    public Task RemoverAsync(string urlRelativa)
    {
        if (string.IsNullOrWhiteSpace(urlRelativa))
            return Task.CompletedTask;

        try
        {
            // Converter URL relativa (/uploads/ano/mes/arquivo.jpg) em caminho físico
            var caminhoRelativo = urlRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var caminhoFisico = Path.Combine(env.ContentRootPath, caminhoRelativo);

            if (File.Exists(caminhoFisico))
            {
                File.Delete(caminhoFisico);
                logger.LogInformation("Arquivo removido: {CaminhoFisico}", caminhoFisico);
            }
            else
            {
                logger.LogWarning("Arquivo não encontrado para remoção: {CaminhoFisico}", caminhoFisico);
            }
        }
        catch (Exception ex)
        {
            // Não propaga — remoção física é best-effort; o soft delete no banco já ocorreu
            logger.LogError(ex, "Erro ao remover arquivo fisicamente: {UrlRelativa}", urlRelativa);
        }

        return Task.CompletedTask;
    }
}
