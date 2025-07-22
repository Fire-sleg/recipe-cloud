using AutoMapper;
using Microsoft.AspNetCore.Identity;
using AuthService.Data.Models.RequestModels;
using AuthService.Entities;
using AuthService.Models;

namespace AuthService.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JWTService _jwtService;
        private readonly IMapper _mapper;
        public UserService(UserManager<ApplicationUser> userManager, IMapper mapper, JWTService jwtService)
        {
            _userManager = userManager;
            _mapper = mapper;
            _jwtService = jwtService;
        }
        public async Task CreateAdmin()
        {
            try
            {
                var admin = new UserRegisterRequest
                {
                    UserName = "admin@recipecloud.com",
                    Email = "admin@recipecloud.com",
                    Password = "******************"
                };
                var user = await _userManager.FindByEmailAsync(admin.Email);
                if (user == null)
                {
                    user = _mapper.Map<ApplicationUser>(admin);
                    await _userManager.CreateAsync(user, admin.Password);
                    var result = await _userManager.FindByEmailAsync(admin.Email);
                    await _userManager.AddToRoleAsync(result, Roles.Admin);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<UserRegisterResponse> RegisterUser(UserRegisterRequest model)
        {
            try
            {
                model.Email = model.Email.ToLower();
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    throw new Exception(/*ErrorMessages.AlreadyRegisteredEmail*/);
                }
                user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    throw new Exception(/*ErrorMessages.AlreadyRegisteredUsername*/);
                }
                user = _mapper.Map<ApplicationUser>(model);
                var res = await _userManager.CreateAsync(user, model.Password);

                if (!res.Succeeded)
                {
                    var errorMessages = res.Errors.Select(e => e.Description).ToList();
                    throw new Exception(string.Join("\n", errorMessages)); 
                }
                var result = await _userManager.FindByEmailAsync(model.Email);
                await _userManager.AddToRoleAsync(result, Roles.Basic);
                return _mapper.Map<UserRegisterResponse>(result);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<UserLoginResponse> LoginUser(UserLoginRequest model)
        {
            try
            {
                model.Email = model.Email.ToLower();
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    throw new Exception(/*ErrorMessages.NoUserWithEmail*/);
                }
                if (!await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    throw new Exception(/*ErrorMessages.WrongPassword*/);
                }
                var token = await _jwtService.GenerateTokenModel(user);

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();

                return new UserLoginResponse(user.Id, user.UserName, role, token);
            }
            catch (Exception)
            {
                throw;
            }
        }


    }
}
