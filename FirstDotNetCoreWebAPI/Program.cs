using FirstDotNetCoreWebAPI.Data;
using FirstDotNetCoreWebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registriere den UserService im DI-Container
builder.Services.AddScoped<UserService>(); // AddScoped erstellt eine neue Instanz pro Anfrage

IConfigurationSection jwtSection = builder.Configuration.GetSection("JWT");
string validIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Issuer is not configured.");
string validAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Audience is not configured.");
string accessTokenSecret = jwtSection["AccessTokenSecret"] ?? throw new InvalidOperationException("Access Token Secret is not configured.");
byte[] accessKey = Convert.FromBase64String(accessTokenSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(accessKey),

        ValidateIssuer = true,
        ValidIssuer = validIssuer,

        ValidateAudience = true,
        ValidAudience = validAudience,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),

        NameClaimType = JwtRegisteredClaimNames.Email,
        RoleClaimType = "token_type"
    };
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<DataContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Default connection is not configured.");
    _ = options.UseSqlServer(connectionString);
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
