using DotNetCoreWebJWTAuthAPI.Data;
using DotNetCoreWebJWTAuthAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DotNetCoreWebJWTAuthAPI.Services
{
    public class TokenService
    {
        private readonly byte[] _accessTokenSecret;
        private readonly byte[] _refreshTokenSecret;
        private readonly string? _issuer;
        private readonly string? _audience;
        private readonly DateTime _issuedAt;
        private readonly DateTime _atExpiresAt;
        private readonly DateTime _rtExpiresAt;
        private readonly DataContext _dataContext;

        public TokenService(IConfiguration configuration, DataContext dataContext)
        {
            // Lese das Secret aus der Umgebungsvariable und dekodiere es von Base64
            string? accessTokenSecret = configuration["JWT:AccessTokenSecret"];
            string? refreshTokenSecret = configuration["JWT:RefreshTokenSecret"];
            _issuer = configuration["JWT:Issuer"];
            _audience = configuration["JWT:Audience"];
            _accessTokenSecret = Convert.FromHexString(accessTokenSecret);
            _refreshTokenSecret = Convert.FromHexString(refreshTokenSecret);
            _issuedAt = DateTime.UtcNow;
            _atExpiresAt = DateTime.UtcNow.AddMinutes(15);
            _rtExpiresAt = DateTime.UtcNow.AddDays(30);
            _dataContext = dataContext;
        }

        public async Task<string> GenerateAccessTokenAsync(string email)
        {
            return await Task.Run(() =>
            {
                JwtSecurityTokenHandler tokenHandler = new();
                SymmetricSecurityKey key = new(_accessTokenSecret);

                SecurityTokenDescriptor tokenDescriptor = new()
                {
                    Subject = new(
                    [
                        new(JwtRegisteredClaimNames.Email, email),
                    ]),

                    Issuer = _issuer,
                    Audience = _audience,
                    Expires = DateTime.UtcNow.AddMinutes(15), // Gültigkeitsdauer des Access-Tokens
                    SigningCredentials = new(key, SecurityAlgorithms.HmacSha512Signature)
                };

                SecurityToken securityToken = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(securityToken);
            });
        }

        public async Task<string?> GenerateRefreshTokenAsync(string email)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            SymmetricSecurityKey key = new(_refreshTokenSecret);

            JwtSecurityToken jwtToken = new(
                issuer: _issuer,
                audience: _audience,
                claims:
                [
                    new("type", "refresh"), // Identifiziert den Token als Refresh-Token
                    new(JwtRegisteredClaimNames.Email, email),
                    new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(_issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                ],
                notBefore: _issuedAt,
                expires: _rtExpiresAt,
                signingCredentials: new(key, SecurityAlgorithms.HmacSha512Signature)
            );

            string token = tokenHandler.WriteToken(jwtToken);

            // Speichere den Refresh-Token in der Datenbank
            await _dataContext.RefreshTokens.AddAsync(new() { Token = token, Email = email, ExpiryDate = _rtExpiresAt, IsRevoked = false, });

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateException) { return null; }

            return token;
        }

        public async Task<bool?> RevokeRefreshTokenAsync(string refreshToken)
        {
            RefreshToken? token = await _dataContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (token == null)
                return false;

            token.IsRevoked = true;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateException) { return null; }

            return true;
        }

        public async Task<TokenValidationResult?> ValidateRefreshToken(string refreshToken)
        {
            JwtSecurityTokenHandler tokenHandler = new();

            try
            {
                TokenValidationResult result = await tokenHandler.ValidateTokenAsync(refreshToken, new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_refreshTokenSecret),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true
                });

                Console.WriteLine($"Token_Validation: {result.IsValid}");

                return !result.IsValid || result.TokenType != "refresh" ? null : result;
            }
            catch
            {
                return null;
            }
        }
    }
}
