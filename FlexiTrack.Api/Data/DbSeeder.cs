using FlexiTrack.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.EnsureCreatedAsync();

        await SeedSystemAdminAsync(userManager);
    }

    private static async Task SeedSystemAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@flexitrack.com";
        const string adminPassword = "Admin123!";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                IsSystemAdmin = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                Console.WriteLine($"Default system admin created: {adminEmail}");
            }
            else
            {
                Console.WriteLine($"Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
