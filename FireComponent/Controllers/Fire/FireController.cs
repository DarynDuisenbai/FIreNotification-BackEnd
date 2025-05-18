using Application.Handlers.NasaHandler;
using Domain.Entities.FireData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Fire
{
    [ApiController]
    [Route("api/[controller]")]
    public class FireController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FireController(IMediator mediator)
        {
            _mediator = mediator;
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
    }
}
