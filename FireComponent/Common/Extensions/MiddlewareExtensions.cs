using HousePassport.Api.Common.Middlewares;

namespace HousePassport.Api.Common.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<CustomExceptionHandler>();
        }
    }
}
