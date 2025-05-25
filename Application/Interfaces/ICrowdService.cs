using Domain.Entities.FireData;

namespace Application.Interfaces
{
    public interface ICrowdService
    {
        Task<List<CrowdSourcingData>> GetDataByUserId(string userId);
    }
}
