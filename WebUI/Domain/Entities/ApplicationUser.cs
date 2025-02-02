using Microsoft.AspNetCore.Identity;

namespace WebUI.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireTime { get; set; }
        public DateTime? CreateDateTime { get; set; }
        public DateTime? LastModifiedDateTime { get; set; }
    }
}
