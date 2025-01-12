using Domain.Models.Identity;

namespace Domain.Models.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<AuthenticationResult> LoginAsync(string username, string code, CancellationToken cancellationToken);
        Task RegisterAsync(string username, CancellationToken cancellationToken);
        Task ResetPasswordAsync(string username, string password, CancellationToken cancellationToken);
        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken);
    }
}
