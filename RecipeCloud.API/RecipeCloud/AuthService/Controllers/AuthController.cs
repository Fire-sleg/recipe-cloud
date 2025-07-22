using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data.Models.RequestModels;
using AuthService.Entities;
using AuthService.Models;
using AuthService.Services;

namespace AuthService.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        
        private readonly IUserService _userservice;
        private readonly UserManager<ApplicationUser> _userManager;
        public AuthController(IUserService userManagementService, UserManager<ApplicationUser> userManager)
        {
            _userservice = userManagementService;
            _userManager = userManager;
        }

        //[Authorize(Roles = Roles.Admin)]
        //[Authorize]
        [HttpPost("register")]
        //[SwaggerResponse(200, "Request_Succeded", typeof(UserRegisterResponse))]
        //[SwaggerResponse(400, "Bad_Request", typeof(string))]
        //[SwaggerResponse(401, "Not_Authorized", typeof(string))]
        //[SwaggerResponse(403, "Access_Not_Allowed", typeof(string))]
        //[SwaggerResponse(500, "Internal_Server_Error", typeof(string))]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest model)
        {
            try
            {
                var user = await _userservice.RegisterUser(model);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("login")]
        //[SwaggerResponse(200, "Request_Succeded", typeof(UserLoginResponse))]
        //[SwaggerResponse(400, "Bad_Request", typeof(string))]
        //[SwaggerResponse(401, "Not_Authorized", typeof(string))]
        //[SwaggerResponse(500, "Internal_Server_Error", typeof(string))]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest model)
        {
            await _userservice.CreateAdmin();
            try
            {
                var user = await _userservice.LoginUser(model);
                //var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [Authorize(Roles = Roles.Basic)]
        //[Authorize(Roles = Roles.Admin)]
        //[Authorize(Roles = Roles.Basic)]
        [HttpGet("getName/{id}")]
        public async Task<IActionResult> GetName(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if(user == null || user.FirstName == null)
            {
                return BadRequest();
            }

            return Ok(user.FirstName);
        }

        [Authorize(Roles = Roles.Standart)]
        [HttpGet("getUser/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return BadRequest();
            }

            return Ok(user);
        }
    }
}
