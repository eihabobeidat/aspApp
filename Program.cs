using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class Program
    {
        public static void Main( string [] args )
        {
            var app = CreateHostBuilder(args).Build();
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            /* try
             {*/
            var context = services.GetRequiredService<DataContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            context.Database.Migrate();
            context.Database.ExecuteSqlRawAsync("DELETE FROM [CONNECTIONS]");
            Seed.SeedUsers(userManager, roleManager).Wait();
            // }
            /* catch ( Exception exception )
             {
                 var logger = services.GetRequiredService<ILogger<Program>>();
                 logger.LogError(exception, "This is a static message ,An Error Occured during migration");
             }*/
            app.Run();
        }

        public static IHostBuilder CreateHostBuilder( string [] args ) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
