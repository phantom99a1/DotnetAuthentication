namespace WebUI.Domain.Response
{
    public class CurrentUserResponse
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime? CreateDateTime { get; set; }
        public DateTime? LastModifiedDateTime { get; set; }
    }
}
