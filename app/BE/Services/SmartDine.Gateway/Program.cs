using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Register YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Health Checks
builder.Services.AddHealthChecks();

// Add CORS Policy for Frontend Dashboard and Mobile App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHttpMetrics();
app.UseWebSockets();

// Map Health Check & Metrics Endpoints
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

// Map YARP Gateway
app.MapReverseProxy();

app.Run();
