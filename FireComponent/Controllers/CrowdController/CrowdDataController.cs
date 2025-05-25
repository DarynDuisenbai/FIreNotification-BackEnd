using Application.DTOs.CrowdSourcing;
using Application.Handlers.NasaHandler;
using Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.CrowdController
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrowdDataController : ControllerBase
    {

        private readonly ICrowdService _crowdService;

        public CrowdDataController(ICrowdService crowdService)
        {
            _crowdService = crowdService;
        }

        [HttpGet(ApiRoutes.CrowdData.GetCrowdDataByUserId)]
        public async Task<IActionResult> GetCrowdDataByUserId([FromQuery] string userId)
        {
            try
            {
                var result = await _crowdService.GetDataByUserId(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}

