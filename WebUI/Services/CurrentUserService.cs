using System.Security.Claims;
using WebUI.Interface;

namespace WebUI.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public string? GetUserId()
        {
            var nameIdentifier = ClaimTypes.NameIdentifier;
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(nameIdentifier);
            return userId;
        }
    }
}
