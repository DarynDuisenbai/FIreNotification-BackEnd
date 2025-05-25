using Application.DTOs.CrowdSourcing;

namespace Application.Interfaces
{
    public interface IPhotoVerificationService
    {
        Task<bool> VerifyAndSavePhotoAsync(VerifyPhotoRequest request);
    }
}
