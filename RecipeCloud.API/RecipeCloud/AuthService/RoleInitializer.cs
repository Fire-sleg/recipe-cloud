using Microsoft.AspNetCore.Identity;
using System.Data;
using AuthService.Entities;

namespace AuthService
{
    public static class RoleInitializer
    {
        public static async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(
                new IdentityRole
                {
                    Name = Roles.Admin
                });
            }
            if (!await roleManager.RoleExistsAsync(Roles.Basic))
            {
                await roleManager.CreateAsync(
                new IdentityRole
                {
                    Name = Roles.Basic
                });
            }
            if (!await roleManager.RoleExistsAsync(Roles.Standart))
            {
                await roleManager.CreateAsync(
                new IdentityRole
                {
                    Name = Roles.Standart
                });
            }
            if (!await roleManager.RoleExistsAsync(Roles.Premium))
            {
                await roleManager.CreateAsync(
                new IdentityRole
                {
                    Name = Roles.Premium
                });
            }
        }
    }
}
