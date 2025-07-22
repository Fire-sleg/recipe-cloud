using AuthService.Data.Models.RequestModels;

namespace AuthService.Services
{
    public interface IUserService
    {
        Task<UserRegisterResponse> RegisterUser(UserRegisterRequest request);
        Task<UserLoginResponse> LoginUser(UserLoginRequest request);
        Task CreateAdmin();
    }

}
