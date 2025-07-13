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
		private readonly string _issuer;
		private readonly string _audience;
		private readonly int _accessTokenExpirationMinutes;
		private readonly int _refreshTokenExpirationDays;
		private readonly DataContext _dataContext;
		private readonly ILogger<TokenService> _logger;

		public TokenService(IConfiguration configuration, DataContext dataContext, ILogger<TokenService> logger)
		{
			_dataContext = dataContext;
			_logger = logger;

			try
			{
                // Lies das Secret aus der Umgebungsvariable und dekodiere es von Base64
                IConfigurationSection jwtSection = configuration.GetSection("JWT");
				string accessTokenSecret = jwtSection["AccessTokenSecret"] ?? throw new InvalidOperationException("Access token secret is not configured.");
				string refreshTokenSecret = jwtSection["RefreshTokenSecret"] ?? throw new InvalidOperationException("Refresh token secret is not configured.");
				_issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Issuer is not configured.");
				_audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Audience is not configured.");
				_accessTokenExpirationMinutes = int.Parse(jwtSection["AccessTokenExpirationMinutes"] ?? "30");
				_refreshTokenExpirationDays = int.Parse(jwtSection["RefreshTokenExpirationDays"] ?? "30");

                _accessTokenSecret = Convert.FromBase64String(accessTokenSecret);
				_refreshTokenSecret = Convert.FromBase64String(refreshTokenSecret);

                _logger.LogInformation("TokenService initialized successfully. AccessTokenSecretLength={AccessTokenLength}, RefreshTokenSecretLength={RefreshTokenLength}",
					_accessTokenSecret.Length, _refreshTokenSecret.Length);
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Failed to initialize TokenService.");
				throw;
			}
		}

		public string GenerateAccessToken(string email)
		{
			_logger.LogInformation("Generating access token for email: {Email}", email);

            var issuedAt = DateTime.UtcNow;
			var atExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

			JwtSecurityTokenHandler tokenHandler = new();
			SymmetricSecurityKey key = new(_accessTokenSecret);

            JwtSecurityToken jwtToken = new(
                issuer: _issuer,
                audience: _audience,
                claims:
                [
					new("token_type", "access"), // Identifiziert den Token als Access-Token
					new(JwtRegisteredClaimNames.Sub, email), // Identifiziert den principal (d.h. den Benutzer) des Tokens
					new(JwtRegisteredClaimNames.Email, email),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Eine eindeutige Kennung für den Token selbst
					new(
                        JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(issuedAt)
                        .ToUnixTimeSeconds()
                        .ToString(),
                        ClaimValueTypes.Integer64
                    )
                ],
                notBefore: issuedAt,
                expires: atExpiresAt,
                signingCredentials: new(key, SecurityAlgorithms.HmacSha512)
            );

            try
            {
				return tokenHandler.WriteToken(jwtToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate access token for email: {Email}", email);
				throw;
			}
		}

		public async Task<string?> GenerateRefreshTokenAsync(string email)
		{
            _logger.LogInformation("Generating refresh token for email: {Email}", email);

            var issuedAt = DateTime.UtcNow;
			var rtExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            JwtSecurityTokenHandler tokenHandler = new();
			SymmetricSecurityKey key = new(_refreshTokenSecret);

			JwtSecurityToken jwtToken = new(
				issuer: _issuer,
				audience: _audience,
				claims:
				[
					new("token_type", "refresh"), // Identifiziert den Token als Refresh-Token
					new(JwtRegisteredClaimNames.Sub, email), // Identifiziert den principal (d.h. den Benutzer) des Tokens
					new(JwtRegisteredClaimNames.Email, email),
					new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Eine eindeutige Kennung für den Token selbst
					new(
						JwtRegisteredClaimNames.Iat,
						new DateTimeOffset(issuedAt)
						.ToUnixTimeSeconds()
						.ToString(),
						ClaimValueTypes.Integer64
					)
				],
				notBefore: issuedAt,
				expires: rtExpiresAt,
				signingCredentials: new(key, SecurityAlgorithms.HmacSha512)
			);

			try
			{
                string tokenString = tokenHandler.WriteToken(jwtToken);
				// Erstelle neuen Datensatz RefreshToken
				await _dataContext.RefreshTokens.AddAsync(new()
				{
					Token = tokenString,
					Email = email,
					ExpiryDate = rtExpiresAt,
					IsRevoked = false,
				});
				// Speichere den Refresh-Token in der Datenbank
				await _dataContext.SaveChangesAsync();

				return tokenString;
			}
			catch (DbUpdateException)
			{
				_logger.LogError("Saving refresh token to database failed!");
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate access token for email: {Email}", email);
				return null;
			}
        }

		// ToDo löschen
		public async Task<bool> CheckRefreshTokenRevoked(string refreshToken)
		{
			RefreshToken? token = await _dataContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);

			if (token == null)
				return false;

			return token.IsRevoked;
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

        public async Task<TokenValidationResult?> ValidateRefreshTokenAsync(string refreshToken)
		{
            _logger.LogInformation("Validating refresh token...");

            JwtSecurityTokenHandler tokenHandler = new();
			SymmetricSecurityKey key = new(_refreshTokenSecret);

			TokenValidationParameters parameters = new()
			{
                ValidateIssuerSigningKey = true,
				IssuerSigningKey = key,

                ValidateIssuer = false,
				ValidIssuer = _issuer,
				
				ValidateAudience = true,
				ValidAudience = _audience,
				
				ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),

				NameClaimType = JwtRegisteredClaimNames.Email,
				RoleClaimType = "token_type",
			};

			try
			{
                // ToDo remove after check why issuer and email are not set but included in the token
                var decodedJWT = tokenHandler.ReadJwtToken(refreshToken);

                //ClaimsPrincipal principal = tokenHandler.ValidateToken(refreshToken, parameters, out SecurityToken validatedToken);
                TokenValidationResult validationResult = await tokenHandler.ValidateTokenAsync(refreshToken, parameters);

    //            if (validatedToken is not JwtSecurityToken jwtToken)
				//{
				//	_logger.LogError("The given token is not a JWT.");
				//	return null;
				//}

				//if (result is not TokenValidationResult validationResult)
				//{
    //                _logger.LogError("The given token is not a JWT.");
    //                return null;
    //            }

				//var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
				//var issuer = jwtToken.Issuer;
				//var audience = jwtToken.Audiences.FirstOrDefault();

				var email = validationResult.Claims.FirstOrDefault(c => c.Key == JwtRegisteredClaimNames.Email).Value;

				if (email is null)
				{
					_logger.LogError("Email claim is missing.");
					return null;
				}

				//var type = jwtToken.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
				string type = (string)validationResult.Claims.FirstOrDefault(c => c.Key == "token_type").Value;

                if (type != "refresh")
				{
					_logger.LogError("The given token is not a refresh token.");
					return null;
				}

				return new()
				{
					IsValid = true,
					SecurityToken = validationResult.SecurityToken,
					ClaimsIdentity = validationResult.ClaimsIdentity
				};
            }
            catch (SecurityTokenValidationException stvex)
            {
                // Token ist formal ungültig (z.B. abgelaufen, Issuer/Audience falsch, Signatur falsch)
                _logger.LogWarning(stvex, "Refresh-Token Validation failed: {Message}", stvex.Message);
                return null;
            }
            catch (ArgumentException argex)
            {
                // z.B. schlechtes Format
                _logger.LogWarning(argex, "Refresh-Token Format invalid: {Message}", argex.Message);
                return null;
            }
            catch (Exception ex)
			{
                _logger.LogError(ex, "Exception occurred during refresh token validation.");
                return null;
			}
		}
	}
}
