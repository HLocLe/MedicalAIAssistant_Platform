using MedMateAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedMateAI.Infrastructure.Persistence.Seeder;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
      

        var roles = new[] { "Admin", "Staff", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@medmate.local";
        var adminPassword = "Admin@12345";
        var adminDisplayName = "System Admin";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = adminDisplayName,
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}

