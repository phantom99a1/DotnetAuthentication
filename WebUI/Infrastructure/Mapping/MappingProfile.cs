using AutoMapper;
using WebUI.Domain.Entities;
using WebUI.Domain.Request;
using WebUI.Domain.Response;

namespace WebUI.Infrastructure.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, UserResponse>();
            CreateMap<ApplicationUser, CurrentUserResponse>();
            CreateMap<UserRegisterRequest, ApplicationUser>();
        }
    }
}
