using System.Net;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Entities;
using System.IdentityModel.Tokens.Jwt;
using MongoDB.Driver;
using Domain.Models.Common.Interfaces;
using Domain.Models.Configuration.Security;
using Domain.Models.Common;
using Domain.Models.Identity;
using Domain.Entities.Identity;

namespace Infrastructure.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<User> _userManager;
        private readonly JwtOptions _jwtOptions;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            UserManager<User> userManager,
            JwtOptions jwtOptions,
            TokenValidationParameters tokenValidationParameters,
            ILogger<IdentityService> logger)
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions;
            _tokenValidationParameters = tokenValidationParameters;
            _logger = logger;
        }
        public async Task<AuthenticationResult> LoginAsync(string username, string code, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidCredentials"));

            return await GenerateAuthenticationResultForUserAsync(user, cancellationToken);

        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
        {
            var validatedToken = GetPrincipalFromToken(token);
            if (validatedToken == null)
                throw new RestException(HttpStatusCode.Unauthorized, GetUserErrorMessage("InvalidToken"));

            var expiryDateUnix = long.Parse(validatedToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("TokenNotExpiredYetError"));

            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            var storedRefreshToken = await DB.Collection<RefreshToken>()
                .Find(x => x.Token == refreshToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (storedRefreshToken == null)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("RefreshTokenDoesNotExist"));

            if (DateTime.UtcNow > storedRefreshToken.DateOfExpiration)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("TokenExpired"));

            if (storedRefreshToken.Invalidated)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidToken"));

            if (storedRefreshToken.Used)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("TokenUsed"));

            if (storedRefreshToken.JwtId != jti)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("RefreshTokenDoesNotMatch"));

            var user = await _userManager.FindByIdAsync(storedRefreshToken.User!.ID);

            await storedRefreshToken.DeleteAsync();
            return await GenerateAuthenticationResultForUserAsync(user, cancellationToken);
        }

        public async Task RegisterAsync(string username, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidCredentials"));
        }

        public async Task<bool> RemoveAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                throw new RestException(HttpStatusCode.NotFound, GetUserErrorMessage("UserNotFound"));

            var removeResult = await _userManager.DeleteAsync(user);
            return removeResult.Succeeded;
        }

        public async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(User user, CancellationToken cancellationToken = default)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtOptions.Secret!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.Firstname),
            };

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_jwtOptions.TokenLifetime),
                SigningCredentials = credentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                User = user.ToReference(),
                DateOfCreation = DateTime.UtcNow,
                DateOfExpiration = DateTime.UtcNow.AddMonths(6),
                Token = Guid.NewGuid().ToString()
            };

            await refreshToken.SaveAsync();

            _logger.LogInformation($"user.Username={user.Firstname} login successfully");

            return new AuthenticationResult
            {
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }

        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var validationTokenParams = _tokenValidationParameters.Clone();
                validationTokenParams.ValidateLifetime = false;
                var principal = tokenHandler.ValidateToken(token, validationTokenParams, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                    return null;

                return principal;
            }
            catch (Exception e)
            {
                _ = e.Message;
                return null;
            }
        }

        private static bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<AuthenticationResult> LoginByPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogWarning($"user.Username={username} not found");
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidCredentials"));
            }

            var passwordHash = GetPasswordHash(password);
            if (user.PasswordHash != passwordHash)
            {
                _logger.LogWarning($"user.Username={username} invalid password");
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidCredentials"));
            }

            return await GenerateAuthenticationResultForUserAsync(user);
        }

        public async Task ResetPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                throw new RestException(HttpStatusCode.BadRequest, GetUserErrorMessage("InvalidCredentials"));

            user.PasswordHash = GetPasswordHash(password);
            await _userManager.UpdateAsync(user);
        }

        private static string GetPasswordHash(string password)
        {
            var alg = SHA512.Create();
            alg.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(alg.Hash!);
        }

        private static string GetUserErrorMessage(string key) => key;
    }

}
