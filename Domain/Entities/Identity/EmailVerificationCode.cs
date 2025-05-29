namespace Domain.Entities.Identity
{
    public class EmailVerificationCode
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
