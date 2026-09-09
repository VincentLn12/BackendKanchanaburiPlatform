global using Core.Entities;
global using Infrastructure;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Identity;
global using Infrastructure.Data;
global using Core.Interfaces;
global using API.Middleware;
global using Infrastructure.Services;
global using KanchanaburiPlatform.Domain.Entities;
global using System.Security.Claims;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Application.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<StoreContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS
builder.Services.AddCors();

// Identity
builder.Services
    .AddIdentityApiEndpoints<AppUser>(options =>
    {
        // Password
        options.Password.RequiredLength = 3;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        // User
        options.User.RequireUniqueEmail = true;

        // Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>();

// Authorization
builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<ShopBusinessService>();

builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(GenericRepository<>));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();


// ========================
// Swagger
// ========================


// เปิด Swagger ทั้ง Development และ Production
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "./v1/swagger.json",
        "Kanchanaburi Platform API V1"
    );

    options.RoutePrefix = "swagger";
});


// ========================
// Middleware
// ========================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseMiddleware<ExceptionMiddleware>();


// ========================
// CORS
// ========================

app.UseCors(x => x
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(
        "http://localhost:5173",
        "https://localhost:5173"
    ));


// ========================
// Authentication
// ========================

app.UseAuthentication();
app.UseAuthorization();


// ========================
// Controllers
// ========================

app.MapControllers();


// ========================
// Identity API
// ========================

app.MapGroup("/api")
    .MapIdentityApi<AppUser>();


// ========================
// Database Migration
// ========================

// ปิดไว้ เพราะจะใช้ Update-Database เอง

// using (var scope = app.Services.CreateScope())
// {
//     try
//     {
//         var context = scope.ServiceProvider
//             .GetRequiredService<StoreContext>();

//         await context.Database.MigrateAsync();
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine(
//             $"Database Migration Error: {ex.Message}"
//         );
//     }
// }


app.Run();