using Application.DTOs.Identity;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto model);
        Task<AuthResponseDto> LoginAsync(LoginDto model);
        Task<bool> ChangeUserRoleAsync(ChangeRole model);
    }
}
