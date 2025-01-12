using System.Net;

namespace Domain.Models.Common
{
    public class RestException : Exception
    {
        public HttpStatusCode Code { get; set; }
        public string? ErrorCode { get; set; }

        public RestException(HttpStatusCode code) : base()
        {
            Code = code;
        }

        public RestException(HttpStatusCode code, string message) : base(message)
        {
            Code = code;
        }
        public RestException(HttpStatusCode code, string message, string errrCode) : base(message)
        {
            Code = code;
            ErrorCode = errrCode;
        }
    }
}
