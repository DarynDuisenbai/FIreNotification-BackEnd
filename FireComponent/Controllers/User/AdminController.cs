using Application.DTOs.Identity;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPut(ApiRoutes.Users.ChangeRole)]
        public async Task<ActionResult<bool>> ChangeRole([FromBody] ChangeRole model)
        {
            try
            {
                if (!model.IsValidRole())
                {
                    return BadRequest(new { message = "Invalid role. Available roles: admin, manager, user" });
                }

                var result = await _authService.ChangeUserRoleAsync(model);

                if (!result)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
