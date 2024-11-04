using DotNetCoreWebJWTAuthAPI.Data;
using DotNetCoreWebJWTAuthAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registriere den UserService im DI-Container
builder.Services.AddScoped<TokenService>(); // AddScoped erstellt eine neue Instanz pro Anfrage
builder.Services.AddScoped<UserService>(); // AddScoped erstellt eine neue Instanz pro Anfrage

builder.Services.AddDbContext<DataContext>(options =>
{
    _ = options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
