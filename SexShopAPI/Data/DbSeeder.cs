using Microsoft.AspNetCore.Identity;
using SexShopAPI.Models;

namespace SexShopAPI.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        string[] roleNames = { "Admin", "Guest" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminUser = await userManager.FindByEmailAsync("admin@sexshop.com");
        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = "admin@sexshop.com",
                Email = "admin@sexshop.com",
            };

            var createPowerUser = await userManager.CreateAsync(user, "Admin@123");
            if (createPowerUser.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                // Category: Toys
                new Product { Name = "Classic Vibrator", Description = "Silky smooth finish, multi-speed.", Price = 29.99m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=Classic+Vibrator" },
                new Product { Name = "Rabbit Vibe", Description = "Dual stimulation calling for fun.", Price = 45.50m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=Rabbit+Vibe" },
                new Product { Name = "G-Spot Wand", Description = "Powerful vibrations where you need them.", Price = 55.00m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=G-Spot+Wand" },
                new Product { Name = "Silicone Dildo", Description = "Realistic feel and easy to clean.", Price = 35.00m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=Silicone+Dildo" },
                new Product { Name = "Anal Plug Set", Description = "Beginner friendly set of 3 sizes.", Price = 25.00m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=Anal+Plug+Set" },
                new Product { Name = "Couple's Ring", Description = "Enhance intimacy for both partners.", Price = 15.00m, Category = "Toys", ImageUrl = "https://placehold.co/300x400?text=Couples+Ring" },
                
                // Category: Lingerie
                new Product { Name = "Lace Teddy", Description = "Elegant red lace teddy.", Price = 39.99m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Lace+Teddy" },
                new Product { Name = "Silk Robe", Description = "Luxurious black silk robe.", Price = 59.99m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Silk+Robe" },
                new Product { Name = "Fishnet Stockings", Description = "Classic fishnet design.", Price = 12.00m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Fishnet+Stockings" },
                new Product { Name = "Leather Harness", Description = "Edgy and bold accessory.", Price = 49.00m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Leather+Harness" },
                new Product { Name = "Satin Panties", Description = "Soft touch satin, various colors.", Price = 18.00m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Satin+Panties" },
                new Product { Name = "Corset Top", Description = "Structuring corset for a defined waist.", Price = 45.00m, Category = "Lingerie", ImageUrl = "https://placehold.co/300x400?text=Corset+Top" },

                // Category: Essentials
                new Product { Name = "Water Based Lube", Description = "Natural feel, non-sticky formula.", Price = 9.99m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Water+Based+Lube" },
                new Product { Name = "Silicone Lube", Description = "Long lasting, waterproof.", Price = 14.99m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Silicone+Lube" },
                new Product { Name = "Massage Oil", Description = "Relaxing lavender scent.", Price = 19.99m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Massage+Oil" },
                new Product { Name = "Toy Cleaner", Description = "Antibacterial spray for toy care.", Price = 8.50m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Toy+Cleaner" },
                new Product { Name = "Condom Pack", Description = "Pack of 12, ultra thin.", Price = 10.00m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Condom+Pack" },
                new Product { Name = "Blindfold", Description = "Soft satin blindfold for sensory play.", Price = 7.00m, Category = "Essentials", ImageUrl = "https://placehold.co/300x400?text=Blindfold" },
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}
