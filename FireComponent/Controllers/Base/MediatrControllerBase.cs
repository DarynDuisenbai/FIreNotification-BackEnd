using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Base
{
    [Route("")]
    public class MediatrControllerBase : ControllerBase
    {
        private IMediator? _mediator;

        protected IMediator? Mediator => _mediator ??= (IMediator?)HttpContext.RequestServices.GetService(typeof(IMediator));
    }
}
