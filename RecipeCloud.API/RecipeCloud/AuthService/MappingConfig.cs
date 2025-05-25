using AutoMapper;
using AuthService.Data.Models.RequestModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AuthService.Models;

namespace AuthService
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UserRegisterRequest, ApplicationUser>();
            CreateMap<ApplicationUser, UserRegisterResponse>();
            CreateMap<ApplicationUser, UserLoginResponse>();
        }
            
    }
}
