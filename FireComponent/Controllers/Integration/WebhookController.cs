using Application.DTOs.Integration;
using Application.Interfaces;
using Application.Service.CrowdService;
using Domain.Entities.Integration;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Integration
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        public WebhookController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost(ApiRoutes.WebHook.AddWebHook)]
        public async Task<IActionResult> AddWebHook([FromQuery] string url, [FromQuery] string displayName)
        {
            try
            {
                var result = await _webhookService.AddWebhookAsync(url, displayName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet(ApiRoutes.WebHook.GetAllWebHook)]
        public async Task<IActionResult> GetAllWebHook()
        {
            try
            {
                var result = await _webhookService.GetAllWebhooksAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut(ApiRoutes.WebHook.UpdateWebhookStatus)]
        public async Task<IActionResult> UpdateWebhookStatus([FromQuery] string id, [FromQuery] bool active)
        {
            try
            {
                var result = await _webhookService.UpdateWebhookStatusAsync(id, active);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut(ApiRoutes.WebHook.UpdateWebhook)]
        public async Task<IActionResult> UpdateWebhook([FromBody] WebhookData req)
        {
            try
            {
                var result = await _webhookService.UpdateWebhookAsync(req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost(ApiRoutes.WebHook.SendNotification)]
        public async Task<IActionResult> SendWebhookNotification([FromBody] WebhookPayload payload)
        {
            try
            {
                await _webhookService.SendWebhookNotificationAsync(
                    payload.Latitude,
                    payload.Longitude,
                    payload.Address,
                    payload.PhotoBase64
                );

                return Ok(new { message = "Уведомления успешно отправлены активным веб-хукам." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Ошибка при отправке уведомлений: {ex.Message}" });
            }
        }

    }
}
