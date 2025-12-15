//MyBooks.AuthService

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using MyBooks.Common.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Configuration;
using System.Text;
using System.Security.Claims;
using MyBooks.AuthService.Services;
using MyBooks.Common.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHost", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8080",
                "https://localhost:8443",
                "https://localhost:8082",
                "http://127.0.0.1:8080",
                "https://qa.mybookcatalog.com",
                "http://host.docker.internal:8080",
                "https://mybookcatalog.com",
                "https://mybookcatalog.com:8443",
                "https://www.mybookcatalog.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();

builder.Configuration.AddMyBooksDefaultProviders(builder.Environment);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth")
));

builder.Services.AddHttpClient<TenantClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:TenantService"] ?? "http://tenants:8080");
});

builder.Services.AddHttpClient<SystemTokenHelper>()
    .AddTypedClient((http, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["ServiceUrls:AuthService"];
        return new SystemTokenHelper(http, baseUrl!);
    });

builder.Services.AddSingleton<HtmlSanitizationService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "MyBooks",
            ValidAudience = "MyBooksUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            RoleClaimType = "role", 
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveUser", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var isActive = context.User.FindFirst("IsActive")?.Value;
            return isActive == "True";
        });
    });
    options.AddPolicy("AdminsOnly", p => p.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy("SuperAdminsOnly", p => p.RequireRole(AppRoles.SuperAdmin));
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

IdentityModelEventSource.ShowPII = true;
var app = builder.Build();

// ensure database is created at startup (for docker/local dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    const int maxRetries = 5;
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            db.Database.Migrate(); // or .Migrate() if you have migrations
            Console.WriteLine("✅ AuthDbContext database created or already exists.");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Attempt {i} failed: {ex.Message}");
            if (i == maxRetries)
            {
                Console.WriteLine("❌ Database init failed after all retries.");
                throw;
            }

            Thread.Sleep(5000); // wait 5 seconds before retry
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("AllowLocalHost");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
