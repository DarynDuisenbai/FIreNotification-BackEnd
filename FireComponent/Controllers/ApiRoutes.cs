namespace WebApi.Controllers
{
    public class ApiRoutes
    {
        public const string Root = "api";
        public const string Base = Root;
        public static class Users
        {
            public const string SendCode = Base + "/users/sendcode";
            public const string Register = Base + "/users/register";
            public const string Login = Base + "/users/login";
            public const string RefreshToken = Base + "/users/token/refresh";
            public const string Profile = Base + "/users/profile";
            public const string Edit = Base + "/users";
            public const string Delete = Base + "/users";
            public const string Seed = Base + "/users/seed";
        }
    }
}
