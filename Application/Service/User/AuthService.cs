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
using Domain.Entities.Identity;

namespace Application.Service.User
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<Domain.Entities.Identity.User> _users;
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly IMongoCollection<EmailVerificationCode> _verificationCodes;
        private readonly IEmailService _emailService;

        public AuthService(
            IOptions<MongoDbSettings> mongoSettings,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<Domain.Entities.Identity.User>("Users");
            _verificationCodes = database.GetCollection<EmailVerificationCode>("EmailVerificationCodes");
            _jwtSettings = jwtSettings;
            _emailService = emailService;
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
            var filter = Builders<Domain.Entities.Identity.User>.Filter.Eq(u => u.Id, model.UserId);
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

        public async Task<bool> SendVerificationCodeAsync(string email)
        {
            try
            {
                // Генерируем 6-значный код
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();

                // Удаляем старые неиспользованные коды для этого email
                await _verificationCodes.DeleteManyAsync(x => x.Email == email && !x.IsUsed);

                // Создаем новый код верификации
                var verificationCode = new EmailVerificationCode
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Email = email,
                    Code = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Код действителен 15 минут
                    IsUsed = false
                };

                // Сохраняем код в базу данных
                await _verificationCodes.InsertOneAsync(verificationCode);

                // Отправляем код на email
                await _emailService.SendVerificationCodeAsync(email, code);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string email, string code)
        {
            try
            {
                // Ищем действительный код
                var verificationCode = await _verificationCodes
                    .Find(x => x.Email == email &&
                              x.Code == code &&
                              !x.IsUsed &&
                              x.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (verificationCode == null)
                {
                    return false; // Код не найден, неверный или истек
                }

                // Помечаем код как использованный
                var filter = Builders<EmailVerificationCode>.Filter.Eq(x => x.Id, verificationCode.Id);
                var update = Builders<EmailVerificationCode>.Update.Set(x => x.IsUsed, true);
                await _verificationCodes.UpdateOneAsync(filter, update);

                // Здесь можно также обновить статус пользователя как "верифицированный"
                var userFilter = Builders<Domain.Entities.Identity.User>.Filter.Eq(u => u.Email, email);
                var userUpdate = Builders<Domain.Entities.Identity.User>.Update.Set(u => u.IsEmailVerified, true);
                await _users.UpdateOneAsync(userFilter, userUpdate);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _users.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return new UserProfileDto
            {
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Photo = user.Photo,
                Roles = user.Roles,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
