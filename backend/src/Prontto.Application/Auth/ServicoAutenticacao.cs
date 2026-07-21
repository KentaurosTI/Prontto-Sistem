using Prontto.Domain.Entities;
using Prontto.Domain.Interfaces;
using Prontto.Application.Common;

namespace Prontto.Application.Auth;

public class ServicoAutenticacao(
    IRepositorioUsuario repositorioUsuarios,
    IRepositorioRefreshToken repositorioRefreshTokens,
    IRepositorioAuditLog repositorioAuditLog,
    IRepositorioCidade repositorioCidades,
    IServicoJwt jwt,
    IHashSenha hashSenha) : IServicoAutenticacao
{
    private const int ExpiracaoRefreshTokenDias = 30;

    public async Task<ResultadoAutenticacao> CadastrarAsync(ComandoCadastro comando)
    {
        if (await repositorioUsuarios.ObterPorEmailAsync(comando.Email.ToLowerInvariant()) is not null)
            throw new ExcecaoConflito("E-mail já cadastrado");

        if (comando.CidadeId.HasValue && !await repositorioCidades.ExisteAsync(comando.CidadeId.Value))
            throw new ExcecaoNaoEncontrado("Cidade não encontrada");

        var novoUsuario = new Usuario
        {
            Nome = comando.Nome.Trim(),
            Email = comando.Email.ToLowerInvariant().Trim(),
            Telefone = comando.Telefone?.Trim(),
            HashSenha = hashSenha.Hashear(comando.Senha),
            TipoConta = comando.TipoConta,
            Especialidade = comando.Especialidade?.Trim(),
            CidadeId = comando.CidadeId,
        };

        var usuarioCriado = await repositorioUsuarios.AdicionarAsync(novoUsuario);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = usuarioCriado.Id,
            Acao = "usuario.cadastro",
            Entidade = "Usuario",
            EntidadeId = usuarioCriado.Id.ToString(),
        });

        var tokenBruto = jwt.GerarRefreshToken();
        var refreshToken = CriarRefreshToken(usuarioCriado.Id, tokenBruto, comando.Ip, comando.UserAgent);
        await repositorioRefreshTokens.AdicionarAsync(refreshToken);

        return new ResultadoAutenticacao(jwt.GerarToken(usuarioCriado), tokenBruto, usuarioCriado);
    }

    public async Task<ResultadoAutenticacao> EntrarAsync(ComandoLogin comando)
    {
        var usuario = await repositorioUsuarios.ObterPorEmailAsync(comando.Email.ToLowerInvariant().Trim())
            ?? throw new ExcecaoNaoAutorizado("E-mail ou senha incorretos");

        if (usuario.DeletadoEm.HasValue)
            throw new ExcecaoNaoAutorizado("E-mail ou senha incorretos");

        if (!hashSenha.Verificar(comando.Senha, usuario.HashSenha))
            throw new ExcecaoNaoAutorizado("E-mail ou senha incorretos");

        var tokenBruto = jwt.GerarRefreshToken();
        var refreshToken = CriarRefreshToken(usuario.Id, tokenBruto, comando.Ip, comando.UserAgent);
        await repositorioRefreshTokens.AdicionarAsync(refreshToken);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = usuario.Id,
            Acao = "usuario.login",
            Entidade = "Usuario",
            EntidadeId = usuario.Id.ToString(),
        });

        return new ResultadoAutenticacao(jwt.GerarToken(usuario), tokenBruto, usuario);
    }

    public async Task<Usuario> ObterUsuarioAtualAsync(Guid idUsuario)
        => await repositorioUsuarios.ObterPorIdAsync(idUsuario)
            ?? throw new ExcecaoNaoEncontrado("Usuário não encontrado");

    public async Task<ResultadoAutenticacao> RenovarSessaoAsync(string refreshTokenBruto, string? ip, string? userAgent)
    {
        var hash = jwt.ComputarHashRefreshToken(refreshTokenBruto);
        var tokenAtual = await repositorioRefreshTokens.ObterPorHashAsync(hash)
            ?? throw new ExcecaoNaoAutorizado("Refresh token inválido");

        if (tokenAtual.EstaRevogado)
        {
            // Token já revogado — possível comprometimento de sessão (FA-03); registrar e lançar
            await repositorioAuditLog.RegistrarAsync(new AuditLog
            {
                UsuarioId = tokenAtual.UsuarioId,
                Acao = "usuario.token_reusado",
                Entidade = "RefreshToken",
                EntidadeId = tokenAtual.Id.ToString(),
            });
            throw new ExcecaoNaoAutorizado("Refresh token revogado");
        }

        if (tokenAtual.EstaExpirado)
            throw new ExcecaoNaoAutorizado("Refresh token expirado");

        var usuario = await repositorioUsuarios.ObterPorIdAsync(tokenAtual.UsuarioId)
            ?? throw new ExcecaoNaoAutorizado("Usuário não encontrado");

        if (usuario.DeletadoEm.HasValue)
            throw new ExcecaoNaoAutorizado("Usuário inativo");

        // Gerar novo par e revogar token atual (rotação obrigatória)
        var novoTokenBruto = jwt.GerarRefreshToken();
        var novoHash = jwt.ComputarHashRefreshToken(novoTokenBruto);

        tokenAtual.RevogadoEm = DateTime.UtcNow;
        tokenAtual.SubstituidoPor = novoHash;
        await repositorioRefreshTokens.AtualizarAsync(tokenAtual);

        var novoRefreshToken = CriarRefreshToken(usuario.Id, novoTokenBruto, ip, userAgent);
        await repositorioRefreshTokens.AdicionarAsync(novoRefreshToken);

        return new ResultadoAutenticacao(jwt.GerarToken(usuario), novoTokenBruto, usuario);
    }

    public async Task LogoutAsync(string refreshTokenBruto)
    {
        var hash = jwt.ComputarHashRefreshToken(refreshTokenBruto);
        var token = await repositorioRefreshTokens.ObterPorHashAsync(hash);

        if (token is null || token.EstaRevogado)
            return; // Idempotente — já revogado ou inexistente

        token.RevogadoEm = DateTime.UtcNow;
        await repositorioRefreshTokens.AtualizarAsync(token);

        await repositorioAuditLog.RegistrarAsync(new AuditLog
        {
            UsuarioId = token.UsuarioId,
            Acao = "usuario.logout",
            Entidade = "Usuario",
            EntidadeId = token.UsuarioId.ToString(),
        });
    }

    private RefreshToken CriarRefreshToken(Guid usuarioId, string tokenBruto, string? ip, string? userAgent)
        => new()
        {
            UsuarioId = usuarioId,
            Token = jwt.ComputarHashRefreshToken(tokenBruto),
            ExpiracaoEm = DateTime.UtcNow.AddDays(ExpiracaoRefreshTokenDias),
            Ip = ip,
            UserAgent = userAgent,
        };
}
