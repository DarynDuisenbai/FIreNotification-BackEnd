using Application.Handlers.User;
using DnsClient;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Base;

namespace WebApi.Controllers.User
{
    [Produces("application/json")]
    [AllowAnonymous]
    public class UserController : MediatrControllerBase
    {
        [HttpPost(ApiRoutes.Users.Register)]
        public async Task<IActionResult> RefreshToken([FromBody] Register.RegisterUserCommand command, CancellationToken cancellationToken)
        {
            return Ok(await Mediator!.Send(command, cancellationToken));
        }
    }
}
