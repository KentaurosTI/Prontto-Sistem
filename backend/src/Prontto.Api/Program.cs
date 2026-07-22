using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prontto.Api.Middlewares;
using Prontto.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AdicionarInfraestrutura(builder.Configuration, builder.Environment);

// ── Upload de arquivos (limite de 5 MB) ───────────────────────────────────────
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_242_880; // 5 MB
});

// ── Swagger ────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prontto API",
        Version = "v1",
        Description = "Marketplace de serviços domésticos — API REST"
    });

    // Suporte a Bearer Token no Swagger UI
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o Access Token JWT. Ex: eyJhbGci..."
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── JWT ────────────────────────────────────────────────────────────────────────
var segredoJwt = builder.Configuration["SESSION_SECRET"];
if (string.IsNullOrWhiteSpace(segredoJwt))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException(
            "SESSION_SECRET é obrigatória em produção. Configure a variável de ambiente antes de iniciar a aplicação.");

    segredoJwt = "prontto-secret-dev-local-32chars!!";
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredoJwt)),
            ValidateIssuer = false,
            ValidateAudience = false,
            // Access Token expira em 15 min; sem tolerância de relógio
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ── Rate Limiting (Microsoft.AspNetCore.RateLimiting) ─────────────────────────
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opcoes.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Muitas requisições. Aguarde um momento antes de tentar novamente." }, token);
    };

    // POST /api/auth/login — 10 req/min por IP
    opcoes.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // POST /api/auth/register — 5 req/min por IP
    opcoes.AddPolicy("cadastro", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // POST /api/servicos/{id}/mensagem — 30 req/min por usuário autenticado
    opcoes.AddFixedWindowLimiter("chat", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // PATCH /cancelar e POST /disputa — 5 req/min por usuário autenticado (requer UseAuthentication antes de UseRateLimiter)
    opcoes.AddPolicy("servicos-criticos", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.User.FindFirstValue("userId") ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(politica => politica
        .WithOrigins("http://localhost:4200", "https://prontto.org", "https://www.prontto.org")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials())); // Necessário para cookies cross-origin em dev

var app = builder.Build();

// ── Migração automática do banco (opcional, para deploy self-contained) ───────
// Ative com AUTO_MIGRATE=true. Aplica as migrations pendentes no startup —
// dispensa rodar `dotnet ef database update` na VPS.
if (builder.Configuration.GetValue<bool>("AUTO_MIGRATE"))
{
    using var scope = app.Services.CreateScope();
    var contexto = scope.ServiceProvider
        .GetRequiredService<Prontto.Infrastructure.Persistence.Context.ContextoBancoDados>();
    contexto.Database.Migrate();
}

// ── Arquivos estáticos de upload ──────────────────────────────────────────────
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Prontto API v1");
    opt.RoutePrefix = "swagger";
});

app.UseMiddleware<MiddlewareExcecao>();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
