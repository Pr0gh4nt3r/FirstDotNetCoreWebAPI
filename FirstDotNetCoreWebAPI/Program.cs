using FirstDotNetCoreWebAPI.Data;
using FirstDotNetCoreWebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registriere den UserService im DI-Container
builder.Services.AddScoped<UserService>(); // AddScoped erstellt eine neue Instanz pro Anfrage

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,                                  // Überprüfe den Aussteller des Tokens
            ValidateAudience = true,                                // Überprüfe den Zielpublikum des Tokens
            ValidateLifetime = true,                                // Überprüfe, ob das Token abgelaufen ist
            ValidateIssuerSigningKey = true,                        // Überprüfe den Signierschlüssel
            ValidIssuer = builder.Configuration["JWT:Issuer"],      // Hier wird der Issuer aus den Einstellungen gelesen
            ValidAudience = builder.Configuration["JWT:Audience"],  // Hier wird die Audience aus den Einstellungen gelesen
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(builder.Configuration["JWT:AccessTokenSecret"]))
        };
    });

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
