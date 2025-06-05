using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SonicPoints.Models;
using System;
using System.Threading.Tasks;

public static class SeedData
{
    public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Superadmin", "Admin", "Manager", "Member" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    public static async Task SeedSuperadmin(UserManager<User> userManager)
    {
        var email = "sakchyamthapa4@gmail.com";
        var password = "test1234!";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var user = new User
            {
                UserName = "Superadmin",
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Superadmin");
            }
        }
    }
}
