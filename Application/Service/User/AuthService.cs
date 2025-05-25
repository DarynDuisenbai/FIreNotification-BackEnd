using Application.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using Infrastructure.Settings;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Application.DTOs.Identity;
using Domain.Entities.Identity.Enums;

namespace Application.Service.User
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<Domain.Entities.Identity.User> _users;
        private readonly IOptions<JwtSettings> _jwtSettings;

        public AuthService(
            IOptions<MongoDbSettings> mongoSettings,
            IOptions<JwtSettings> jwtSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<Domain.Entities.Identity.User>("Users");
            _jwtSettings = jwtSettings;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto model)
        {
            if (await _users.Find(x => x.Email == model.Email).AnyAsync())
            {
                throw new Exception("User with this email already exists");
            }

            var user = new Domain.Entities.Identity.User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Username = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Roles = model.Role,
                CreatedAt = DateTime.UtcNow
            };

            await _users.InsertOneAsync(user);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                UserId = user.Id
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto model)
        {
            var user = await _users.Find(x => x.Email == model.Email).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password");
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                UserId = user.Id
            };
        }

        private string GenerateJwtToken(Domain.Entities.Identity.User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpirationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<bool> ChangeUserRoleAsync(ChangeRole model)
        {
            var filter = Builders<Domain.Entities.Identity.User>.Filter.Eq(u => u.Username, model.Username);
            var update = Builders<Domain.Entities.Identity.User>.Update.Set(u => u.Roles, model.Role);

            var result = await _users.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
        public async Task<bool> UploadProfilePhoto(EditProfilePhotoDto req)
        {
            var filter = Builders<Domain.Entities.Identity.User>.Filter.Eq(u => u.Id, req.UserId);
            var update = Builders<Domain.Entities.Identity.User>.Update.Set(u => u.Photo, req.Photo);

            var result = await _users.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

    }
}
