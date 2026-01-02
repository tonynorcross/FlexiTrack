using System.Text;
using FlexiTrack.Api.Authorization;
using FlexiTrack.Api.Data;
using FlexiTrack.Api.Data.Entities;
using FlexiTrack.Api.Features.Companies;
using FlexiTrack.Api.Features.Health;
using FlexiTrack.Api.Features.TaskItems;
using FlexiTrack.Api.Features.Tasks;
using FlexiTrack.Api.Features.Users;
using FlexiTrack.Api.Services;
using FlexiTrack.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(AuthorizationPolicies.AddPolicies);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<IBankHolidayService, BankHolidayService>();

builder.Services.AddOpenApi();
builder.Services.AddMediator(typeof(Program).Assembly);

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".ts"] = "application/javascript";
provider.Mappings[".tsx"] = "application/javascript";
provider.Mappings[".jsx"] = "application/javascript";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.MapHealthEndpoints();
app.MapUsersEndpoints();
app.MapCompaniesEndpoints();
app.MapTasksEndpoints();
app.MapTaskItemsEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
