using AuthService.Models;
using AuthService.Models.DTOs;
using AuthService.Repository;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/preferences")]
    public class PreferencesController : ControllerBase
    {
        private readonly IUserPreferencesRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public PreferencesController(IUserPreferencesRepository repo, UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _repo = repo;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> Get(Guid userId)
        {
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var prefs = await _repo.GetByUserIdAsync(userId);
            //if (prefs == null) return NotFound();
            if (prefs == null) 
            {
                prefs = new UserPreferences() { UserId = userId };
            }
            return Ok(prefs);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserPreferencesDTO preferencesDTO)
        {
            if (preferencesDTO == null) return BadRequest();

            var preferences = _mapper.Map<UserPreferences>(preferencesDTO);
            await _repo.SaveAsync(preferences);
            return Ok();
        }
    }

}
