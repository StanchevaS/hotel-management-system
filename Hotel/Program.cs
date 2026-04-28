using Hotel.Data;
using Hotel.Models;
using Hotel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IHotelService, HotelService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var hotelService = services.GetRequiredService<IHotelService>();

    string[] roles = { "Administrator", "Receptionist" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "admin@hotel.bg";
    var adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }
    else
    {
        var rolesForAdmin = await userManager.GetRolesAsync(adminUser);
        if (!rolesForAdmin.Contains("Administrator"))
        {
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }

    var receptionistEmail = "reception@hotel.bg";
    var receptionistPassword = "Reception123!";

    var receptionistUser = await userManager.FindByEmailAsync(receptionistEmail);
    if (receptionistUser == null)
    {
        receptionistUser = new ApplicationUser
        {
            UserName = receptionistEmail,
            Email = receptionistEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(receptionistUser, receptionistPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(receptionistUser, "Receptionist");
        }
    }
    else
    {
        var rolesForReceptionist = await userManager.GetRolesAsync(receptionistUser);
        if (!rolesForReceptionist.Contains("Receptionist"))
        {
            await userManager.AddToRoleAsync(receptionistUser, "Receptionist");
        }
    }

    await hotelService.AutoCompleteExpiredReservationsAsync();
    await hotelService.RecalculateAllRoomStatusesAsync();
}

app.Run();