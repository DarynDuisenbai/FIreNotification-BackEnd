using Domain.Entities.Integration;

namespace Application.Interfaces
{
    public interface IWebhookService
    {
        Task SendWebhookNotificationAsync(double latitude, double longitude, string address, string photoBase64);
        Task <WebhookData> AddWebhookAsync(string url, string displayName);
        Task<WebhookData> UpdateWebhookStatusAsync(string id, bool active);
        Task<List<WebhookData>> GetAllWebhooksAsync();
        Task<WebhookData> UpdateWebhookAsync(WebhookData model);
    }
}
