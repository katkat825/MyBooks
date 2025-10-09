//MyBooks.TenantService

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyBooks.TenantService.Models;
using MyBooks.TenantService.Validators;
using MyBooks.TenantService.Data;
using MyBooks.Common.Services;
using MyBooks.Common.Helpers;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MyBooks.TenantService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHost", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8080",
                "https://localhost:8443",
                "http://127.0.0.1:8080",
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

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "tenant")
    ));

builder.Services.AddSingleton<HtmlSanitizationService>();

builder.Services.AddValidatorsFromAssemblyContaining<TenantValidator>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    options.JsonSerializerOptions.WriteIndented = true;
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing from configuration");
}

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
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
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<AuthClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:AuthService"] ?? "http://auth:8080");
});
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:CatalogService"] ?? "http://catalog:8080");
});
builder.Services.AddHttpClient<SystemTokenHelper>()
    .AddTypedClient((http, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["ServiceUrls:AuthService"];
        return new SystemTokenHelper(http, baseUrl!);
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ensure database is created at startup (for docker/local dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    const int maxRetries = 5;
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            db.Database.Migrate(); // or .Migrate() if you have migrations
            Console.WriteLine("✅ TenantDbContext database created or already exists.");
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

app.UseRouting();
app.UseCors("AllowLocalHost");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();