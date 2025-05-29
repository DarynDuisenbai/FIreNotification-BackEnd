using Application.DTOs.EmailDto;
using Application.DTOs.Identity;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost(ApiRoutes.Users.Register)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
        {
            try
            {
                var result = await _authService.RegisterAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost(ApiRoutes.Users.Login)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
        {
            try
            {
                var result = await _authService.LoginAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut(ApiRoutes.Users.UploadProfilePhoto)]
        public async Task<ActionResult<bool>> UploadProfilePhoto([FromBody] EditProfilePhotoDto req)
        {
            try
            {
                var result = await _authService.UploadProfilePhoto(req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost(ApiRoutes.Users.SendVerificationCode)]
        public async Task<ActionResult<object>> SendVerificationCode([FromBody] SendVerificationCodeDto model)
        {
            try
            {
                var result = await _authService.SendVerificationCodeAsync(model.Email);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Код верификации отправлен на ваш email"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Не удалось отправить код верификации"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost(ApiRoutes.Users.VerifyEmail)]
        public async Task<ActionResult<object>> VerifyEmail([FromBody] VerifyEmailDto model)
        {
            try
            {
                var result = await _authService.VerifyEmailAsync(model.Email, model.Code);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Email успешно верифицирован"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверный код или код истек"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet(ApiRoutes.Users.GetProfile)]
        public async Task<ActionResult<bool>> GetProfile([FromQuery] string userId)
        {
            try
            {
                var result = await _authService.GetProfileAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
