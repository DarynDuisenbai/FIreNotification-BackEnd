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
            public const string UploadProfilePhoto = Base + "/users/uploadProfilePhoto";
            public const string SendVerificationCode = Base + "/users/sendCodeEmail";
            public const string VerifyEmail = Base + "/users/verifyEmail";
            public const string GetProfile = Base + "/users/getProfile";
        }
        public static class Fire
        {
            public const string GetFiresByDate = Base + "/fires/fireByDate";
            public const string SaveCrowdData = Base + "/fires/saveCrowdData";
        }
        public static class CrowdData
        {
            public const string GetCrowdDataByUserId = Base + "/crowd/user/firedata";
        }
        public static class WebHook
        {
            public const string AddWebHook = Base + "/webhook/add";
            public const string GetAllWebHook = Base + "/webhook/getAll";
            public const string UpdateWebhookStatus = Base + "/webhook/updateStatus";
            public const string UpdateWebhook = Base + "/webhook/update";
            public const string SendNotification = Base + "/webhook/sendNotification";
        }
    }
}
