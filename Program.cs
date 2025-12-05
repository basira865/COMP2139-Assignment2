using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using COMP2139_Assignment1_1.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog; // ✅ ADD

// ✅ PART 4: Configure Serilog FIRST (before builder)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // ✅ PART 4: Add Serilog to the builder
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllersWithViews();

    // Session support
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        
        // Email confirmation
        options.SignIn.RequireConfirmedEmail = false;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        
        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Email sender
    builder.Services.AddTransient<IEmailSender, EmailSender>();

    // Cookie settings
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

    var app = builder.Build();

    // Seed roles and admin user
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await SeedData.Initialize(services);
    }

    // ✅ PART 4: Add Serilog request logging
    app.UseSerilogRequestLogging();

    // ✅ PART 4: Global Error Handling Middleware
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error"); // Handles 500 errors
        app.UseStatusCodePagesWithReExecute("/Home/StatusCode?code={0}");// Handles 404
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    // ⭐⭐⭐ CRITICAL: Authentication must come BEFORE Authorization ⭐⭐⭐
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSession();

    // Routing
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}