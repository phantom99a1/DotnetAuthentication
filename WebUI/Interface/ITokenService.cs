using WebUI.Domain.Entities;

namespace WebUI.Interface
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
