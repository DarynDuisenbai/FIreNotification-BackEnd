using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Net;
using FluentValidation;
using Domain.Models.Common;
using Domain.Models.Common.Interfaces;

namespace Application.Handlers.User
{
    public class Register
    {
        public class RegisterUserCommand : IRequest<Unit>
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class Validator : AbstractValidator<RegisterUserCommand>
        {
            public Validator()
            {
                RuleFor(x => x.UserName).Length(11);
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public class Handler : IRequestHandler<RegisterUserCommand, Unit>
        {
            private readonly IIdentityService _identityService;
            private readonly UserManager<Domain.Entities.Identity.User> _userManager;

            public Handler(
                IIdentityService identityService, UserManager<Domain.Entities.Identity.User> userManager)
            {
                _identityService = identityService;
                _userManager = userManager;

            }

            public async Task<Unit> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                var existingUser = await _userManager.FindByNameAsync(request.UserName);
                if (existingUser != null)
                    throw new RestException(HttpStatusCode.NotFound, "UserAlreadyCreated");

                await _identityService.RegisterAsync(request.UserName, cancellationToken);
                await _identityService.ResetPasswordAsync(request.UserName, request.Password, cancellationToken);

                return Unit.Value;
            }
        }
    }

}
