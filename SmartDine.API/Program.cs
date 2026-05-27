using SmartDine.Application.Services;
using SmartDine.Domain.Interfaces;
using SmartDine.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ===== Dependency Injection (DI) =====
// Đăng ký IOrderRepository → InMemoryOrderRepository (Singleton vì dùng RAM)
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// Đăng ký OrderService
builder.Services.AddScoped<OrderService>();

// Thêm Controllers
builder.Services.AddControllers();

// Swagger cho API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
