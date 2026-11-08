using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalAgent.Infrastructure.Data;
using PersonalAgent.Infrastructure.Identity;
using PersonalAgent.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IIssueService, IssueService>();
builder.Services.AddScoped<ICommentService, CommentService>();

var connectionString =
    builder.Configuration.GetConnectionString("MvcDatabase")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:MvcDatabase mangler.");

builder.Services.AddDbContext<MvcDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddDefaultIdentity<MvcApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MvcDbContext>();

var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
    var adminEmail = builder.Configuration["SeedAdmin:Email"];
    var adminPassword = builder.Configuration["SeedAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        await MvcDbSeeder.SeedAdminAsync(
            app.Services,
            adminEmail,
            adminPassword);
    }
    
    
    var demoEmail = builder.Configuration["SeedDemo:Email"];
    var demoPassword = builder.Configuration["SeedDemo:Password"];

    if (!string.IsNullOrWhiteSpace(demoEmail) &&
        !string.IsNullOrWhiteSpace(demoPassword))
    {
        await MvcDbSeeder.SeedDemoUserAsync(
            app.Services,
            demoEmail,
            demoPassword);
    }
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

app.Run();

public partial class Program { }