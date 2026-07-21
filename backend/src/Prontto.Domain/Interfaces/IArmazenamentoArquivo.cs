namespace Prontto.Domain.Interfaces;

/// <summary>
/// Abstração para armazenamento de arquivos enviados por upload.
/// Implementações podem usar disco local, S3, Azure Blob, etc.
/// </summary>
public interface IArmazenamentoArquivo
{
    /// <summary>
    /// Persiste o conteúdo do arquivo e retorna a URL relativa de acesso.
    /// </summary>
    /// <param name="conteudo">Stream com o conteúdo do arquivo.</param>
    /// <param name="nomeOriginal">Nome original do arquivo (usado para extrair a extensão).</param>
    /// <param name="contentType">MIME type declarado pelo cliente.</param>
    /// <returns>URL relativa para acessar o arquivo (ex: /uploads/2026/06/abc123.jpg).</returns>
    Task<string> SalvarAsync(Stream conteudo, string nomeOriginal, string contentType);

    /// <summary>
    /// Remove o arquivo fisicamente. Não lança exceção se o arquivo não existir.
    /// </summary>
    /// <param name="urlRelativa">URL relativa retornada por <see cref="SalvarAsync"/>.</param>
    Task RemoverAsync(string urlRelativa);
}
