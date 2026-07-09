using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartDine.AI.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== Authentication & Authorization (RSA256 Public Key) =====
var rsaPublicKeyBase64 = builder.Configuration["Jwt:RsaPublicKey"];
RSA? rsaPublicKey = null;
if (!string.IsNullOrEmpty(rsaPublicKeyBase64))
{
    rsaPublicKey = RSA.Create();
    rsaPublicKey.ImportRSAPublicKey(Convert.FromBase64String(rsaPublicKeyBase64), out _);
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = rsaPublicKey != null ? new RsaSecurityKey(rsaPublicKey) : null
    };
});

builder.Services.AddAuthorization();

// ===== Health Checks =====
builder.Services.AddHealthChecks()
    .AddCheck("ollama-health", async () =>
    {
        var httpClient = new HttpClient();
        var ollamaUrl = builder.Configuration["Services:Ollama"];
        try
        {
            var response = await httpClient.GetAsync($"{ollamaUrl}/api/tags");
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama service is accessible")
                : HealthCheckResult.Unhealthy($"Ollama service returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Ollama service health check failed: {ex.Message}");
        }
    });

// ===== HTTP Client Factory =====
builder.Services.AddHttpClient();

// ===== Controllers =====
builder.Services.AddControllers();

// ===== Swagger Setup with JWT Auth =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartDine AI API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// ===== Middleware Pipeline =====
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
