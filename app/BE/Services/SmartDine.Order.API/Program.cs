using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SmartDine.Infrastructure.Persistence;
using SmartDine.Order.API.BackgroundServices;
using SmartDine.Order.API.Middleware;
using SmartDine.Order.API.Hubs;
using SmartDine.Order.API.Services;
using SmartDine.Application;
using SmartDine.Infrastructure;
using SmartDine.Infrastructure.ExternalServices;
using SmartDine.Domain.Interfaces;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ===== Application & Infrastructure Services =====
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

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
    .AddDbContextCheck<SmartDineDbContext>();

// ===== Controllers & SignalR =====
builder.Services.AddControllers();
builder.Services.AddSignalR();

// ===== Real-time & Caching Services =====
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddDistributedMemoryCache();

// ===== Payment Gateway (PayOS) =====
builder.Services.AddHttpClient<IPaymentGateway, PayOsGateway>();

// ===== Background Jobs =====
builder.Services.AddHostedService<PaymentExpiryJob>();

// ===== Swagger Setup with JWT Auth =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartDine Order API", Version = "v1" });

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

// ===== Auto Migrate and Seed Database =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SmartDineDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var seeder = services.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// ===== Middleware Pipeline =====
app.UseHttpMetrics();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
