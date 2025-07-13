using DotNetCoreWebJWTAuthAPI.Data;
using DotNetCoreWebJWTAuthAPI.Entities;
using DotNetCoreWebJWTAuthAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace DotNetCoreWebJWTAuthAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(DataContext dataContext, TokenService tokenService, UserService userService) : ControllerBase
    {
        private readonly DataContext _dataContext = dataContext;
        private readonly TokenService _tokenService = tokenService;
        private readonly UserService _userService = userService;

        [HttpPost("token")]
        public async Task<ActionResult> GenerateTokens(User _user)
        {
            User? user = await _userService.ValidateUserAsync(_user.Email, _user.Password);

            if (user == null)
                return Unauthorized("Invalid email or password.");

            string accessToken = _tokenService.GenerateAccessToken(_user.Email);
            string? refreshToken = await _tokenService.GenerateRefreshTokenAsync(_user.Email);

            if (refreshToken == null)
                return BadRequest("Something went wrong while generating the refresh token.");

            // Beide Tokens werden zurückgegeben
            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult> RefreshToken([FromBody] string refreshToken)
        {
            bool isTokenRevoked = await _tokenService.CheckRefreshTokenRevoked(refreshToken);

            if (isTokenRevoked)
                return NotFound("Token not found or already revoked.");

            TokenValidationResult? result = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

            if (result == null)
                return Unauthorized("Invalid refresh token.");

            string? email = result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

            if (email == null)
                return Unauthorized("Email is missing.");

            string newAccessToken = _tokenService.GenerateAccessToken(email);
            string? newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(email);

            bool? success = await _tokenService.RevokeRefreshTokenAsync(refreshToken);

            if (success == false)
                return NotFound("Token not found or already revoked.");

            if (success == null)
                return BadRequest("Something went wrong while revoking the refresh token.");

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        [HttpPatch("revoke")]
        public async Task<ActionResult> RevokeToken([FromBody] string refreshToken)
        {
            bool? success = await _tokenService.RevokeRefreshTokenAsync(refreshToken);

            if (success == false)
                return NotFound("Token not found or already revoked.");

            if (success == null)
                return BadRequest("Something went wrong while revoking the refresh token.");

            return Ok("Refresh token revoked successfully.");
        }
    }
}
