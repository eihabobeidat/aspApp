using API.Entities;
using API.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers( UserManager<AppUser> userManager, RoleManager<AppRole> roleManager )
        {
            if ( !userManager.Users.Any() )
            {
                //Members Seed
                var usersData = File.ReadAllText("Data/UserSeedData.json");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new DateOnlyJsonConverter());
                var users = JsonSerializer.Deserialize<List<AppUser>>(usersData, options);
                var roles = new List<AppRole>
                {
                    new AppRole {Name = "Member"},
                    new AppRole {Name = "Admin"},
                    new AppRole {Name = "Moderator"}
                };

                foreach ( var role in roles )
                {
                    await roleManager.CreateAsync(role);
                }

                foreach ( var user in users )
                {
                    user.UserName = user.UserName.ToLower();
                    await userManager.CreateAsync(user, "Test@123");
                    await userManager.AddToRoleAsync(user, "Member");
                    //using var hmac = new HMACSHA512();
                    //user.Password = hmac.ComputeHash(Encoding.UTF8.GetBytes("Test@123"));
                    //user.PasswordSalt = hmac.Key;
                    //context.Users.AddAsync(user);
                }



                //Admin Seed
                var adminUser = new AppUser
                {
                    UserName = "admin",
                    KnownAs = "Dean"
                };

                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRolesAsync(adminUser, new [] { "Admin", "Moderator" });



                //Moderator Seed
                var moderatorUser = new AppUser
                {
                    UserName = "moderator",
                    KnownAs = "Sam"
                };

                await userManager.CreateAsync(moderatorUser, "Moderator@123");
                await userManager.AddToRoleAsync(moderatorUser, "Moderator");
                
                // context.SaveChanges();
            }
        }
    }
}
