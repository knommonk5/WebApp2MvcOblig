using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PersonalAgent.Api.Data;
using PersonalAgent.Api.Models;
using PersonalAgent.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("AgentDatabase")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:AgentDatabase mangler. Sett den med user-secrets.");

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity håndterer kontoer, passord og tokeninnlogging.
builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<AgentDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Personal Agent Core API",
        Version = "v1",
        Description = "API for personlige interesser, preferanser og overvåkingsregler."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Lim inn accessToken fra innloggingen. Ikke skriv Bearer foran."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddScoped<InterestService>();
builder.Services.AddScoped<PreferenceService>();
builder.Services.AddScoped<InterestService>();
builder.Services.AddScoped<PreferenceService>();
builder.Services.AddScoped<WatchRuleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Registrerer Identitys ferdige autentiseringsendepunkter.
app.MapGroup("/api/auth")
    .MapIdentityApi<ApplicationUser>();

app.MapControllers();

app.Run();

public partial class Program { }