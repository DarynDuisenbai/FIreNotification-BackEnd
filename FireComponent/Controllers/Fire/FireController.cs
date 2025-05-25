using Application.DTOs.CrowdSourcing;
using Application.Handlers.NasaHandler;
using Application.Interfaces;
using Application.Service.User;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Fire
{
    [ApiController]
    [Route("api/[controller]")]
    public class FireController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPhotoVerificationService _photoVerificationService;

        public FireController(IMediator mediator, IPhotoVerificationService photoVerificationService)
        {
            _mediator = mediator;
            _photoVerificationService = photoVerificationService;
        }

        [HttpGet(ApiRoutes.Fire.GetFiresByDate)]
        public async Task<IActionResult> GetFiresByDate([FromQuery] string date, CancellationToken cancellationToken)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest("Invalid date format. Use yyyy-MM-dd.");
            }

            var result = await _mediator.Send(new GetFiresByDateCommand(parsedDate), cancellationToken);
            return Ok(result);
        }

        [HttpPost(ApiRoutes.Fire.SaveCrowdData)]
        public async Task<IActionResult> SaveCrowdData([FromBody] VerifyPhotoRequest req, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _photoVerificationService.VerifyAndSavePhotoAsync(req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
