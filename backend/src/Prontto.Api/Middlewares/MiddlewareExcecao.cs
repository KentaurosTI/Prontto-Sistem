using Prontto.Application.Common;

namespace Prontto.Api.Middlewares;

public class MiddlewareExcecao(RequestDelegate proximo, ILogger<MiddlewareExcecao> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await proximo(ctx);
        }
        catch (ExcecaoNaoEncontrado ex)
        {
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ExcecaoConflito ex)
        {
            ctx.Response.StatusCode = 409;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ExcecaoNaoAutorizado ex)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ExcecaoProibido ex)
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ExcecaoValidacao ex)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ExcecaoTransicaoInvalida ex)
        {
            ctx.Response.StatusCode = 422;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new { error = "Erro interno do servidor" });
        }
    }
}
