//MyBooks.AuthService

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Configuration;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHost", policy =>
    {
        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithOrigins("http://localhost:62194")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();

builder.Configuration.AddMyBooksDefaultProviders(builder.Environment);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection")));

//add security services from Common
builder.Services.AddSingleton<HtmlSanitizationService>();

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
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", 
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = (ClaimsIdentity)context.Principal.Identity;
                var roleClaim = identity.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

                if (roleClaim != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value)); // ?? Force-add role claim
                    Console.WriteLine($"? Role '{roleClaim.Value}' added manually.");
                }
                else
                {
                    Console.WriteLine("? No role claim found!");
                }

                return Task.CompletedTask;
            }
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

IdentityModelEventSource.ShowPII = true;
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocalHost");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AuthDbContext>();

    dbContext.Database.Migrate();

    var adminEmail = "admin@wtf.com";
    var adminExists = dbContext.Users.Any(u => u.Email == adminEmail);

    if(!adminExists)
    {
        var adminUser = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = adminEmail,
            Role = "Admin",
            AgeCategoryId = 3,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
        };

        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();
    }
}

app.Run();
