using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Entities;
using AuthService.Models;

namespace AuthService.Services
{
    public class JWTService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthentificationSettings _options;
        private readonly int Access_Token_Lifetime = 2;

        public JWTService(UserManager<ApplicationUser> userManager, IOptions<AuthentificationSettings> options)
        {
            _userManager = userManager;
            _options = options.Value;
        }
        public JwtSecurityToken CreateToken(List<Claim> claims, int lifetime)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityStringKey));
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
            expires: DateTime.UtcNow.AddHours(lifetime),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );
            return token;
        }
        public async Task<List<Claim>> GetClaims(ApplicationUser user)
        {
            var claims = new List<Claim> {
                new Claim("ident", user.Id),
                new Claim("email",user.Email),
            };
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role  , role));
            }
            return claims;
        }
        /*ClaimsIdentity.DefaultRoleClaimType*/
        public async Task<Token> GenerateTokenModel(ApplicationUser user)
        {
            var claims = await GetClaims(user);
            var accessToken = CreateToken(claims, Access_Token_Lifetime);
            var token = new Token()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
                AccessTokenExpireDate = DateTime.UtcNow.AddHours(Access_Token_Lifetime),
            };
            return token;
        }

    }
}
