//MyBooks.AuthService

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;
using System.Text;

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocalHost");

app.UseHttpsRedirection();

app.UseAuthorization();

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
