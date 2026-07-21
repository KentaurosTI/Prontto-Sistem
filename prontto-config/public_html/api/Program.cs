using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Leitura das variáveis de ambiente (sobrescreve appsettings.json)
string stripeSecretKey = builder.Configuration["STRIPE_SECRET_KEY"]
                      ?? throw new InvalidOperationException("STRIPE_SECRET_KEY não configurada");
string stripePublishableKey = builder.Configuration["STRIPE_PUBLISHABLE_KEY"]
                      ?? throw new InvalidOperationException("STRIPE_PUBLISHABLE_KEY não configurada");

// Configuração do Stripe
builder.Services.AddSingleton<StripeClient>(sp => new StripeClient(stripeSecretKey));

// Configuração do JWT
string jwtSecret = builder.Configuration["Jwt:Secret"]
               ?? throw new InvalidOperationException("JWT_SECRET_KEY não configurada");
string jwtIssuer = builder.Configuration["Jwt:Issuer"]
               ?? throw new InvalidOperationException("JWT_ISSUER não configurado");
string jwtAudience = builder.Configuration["Jwt:Audience"]
               ?? throw new InvalidOperationException("JWT_AUDIENCE não configurado");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Serviços padrão
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuração do pipeline de requisições
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();