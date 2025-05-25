namespace WebApi.Controllers
{
    public class ApiRoutes
    {
        public const string Root = "api";
        public const string Base = Root;

        public static class Users
        {
            public const string Register = Base + "/users/register";
            public const string Login = Base + "/users/login";
            public const string ChangeRole = Base + "/users/changeRole";
        }
        public static class Fire
        {
            public const string GetFiresByDate = Base + "/fires/fireByDate";
            public const string SaveCrowdData = Base + "/fires/saveCrowdData";
        }
    }
}
