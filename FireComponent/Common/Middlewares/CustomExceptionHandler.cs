using HousePassport.Domain.Models.Common;
using Newtonsoft.Json;
using System.Net;

namespace HousePassport.Api.Common.Middlewares
{
    public class CustomExceptionHandler
    {
        private readonly RequestDelegate _next;

        public CustomExceptionHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, ILogger<CustomExceptionHandler> logger)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex, logger);
            }
        }

        private Task HandleExceptionAsync(HttpContext httpContext, Exception exception, ILogger<CustomExceptionHandler> logger)
        {
            var code = HttpStatusCode.InternalServerError;
            string result;

            switch (exception)
            {
                case RestException restException:
                    code = restException.Code;
                    result = JsonConvert.SerializeObject(new ErrorResponse
                    {
                        Message = restException.Message,
                        Code=restException.ErrorCode,
                       
                    });
                    break;
                default:
                    logger.LogError(exception, "Unhandled exception occured: " + exception.Message + exception.StackTrace);
                    result = JsonConvert.SerializeObject(new ErrorResponse
                    {
                        Message = exception.Message
                    });
                    break;
            }

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)code;

            return httpContext.Response.WriteAsync(result);
        }
    }
}
