﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SonicPoints.Data;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:5085");
QuestPDF.Settings.License = LicenseType.Community;

// ------------------ DATABASE & IDENTITY ------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ------------------ JWT CONFIG ------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key is missing in configuration!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});

// ------------------ AUTHORIZATION ------------------
builder.Services.AddAuthorization();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ------------------ CORS ------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
     "http://127.0.0.1:5500",
                "http://localhost:5500",
                "https://main.d3amolag588ltp.amplifyapp.com")
             .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ------------------ FORM UPLOAD LIMITS ------------------
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 30 * 1024 * 1024; // 30 MB
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30 MB
});

// ------------------ SWAGGER ------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SonicPoints API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value.Errors.Select(x => x.ErrorMessage).ToArray()
            );
        return new BadRequestObjectResult(new { Message = "Validation Failed", Errors = errors });
    };
});

// ------------------ DEPENDENCIES ------------------
builder.Services.AddTransient<IProjectRepository, ProjectRepository>();
builder.Services.AddTransient<ITaskRepository, TaskRepository>();
builder.Services.AddTransient<IRewardRepository, RewardRepository>();
builder.Services.AddTransient<ILeaderboardRepository, LeaderboardRepository>();
builder.Services.AddTransient<IRedeemableItemRepository, RedeemableItemRepository>();
builder.Services.AddTransient<IProjectAuthorizationService, ProjectAuthorizationService>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();


builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
var app = builder.Build();

// ------------------ GLOBAL EXCEPTION HANDLING (for large files) ------------------
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (BadHttpRequestException ex)
    {
        context.Response.StatusCode = 413;
        await context.Response.WriteAsync("Upload failed: File too large or malformed request.");
    }
});
// ------------------ ROLE + SUPERADMIN SEEDING ------------------
async Task EnsureRolesAndSuperadminCreatedAsync(WebApplication app)
{
    Console.WriteLine("🔥 Seeding started");

    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    string[] roles = { "Admin", "Manager", "Checker", "Member", "SuperAdmin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"✅ Created role: {role} = {result.Succeeded}");
        }
        else
        {
            Console.WriteLine($"ℹ️ Role exists: {role}");
        }
    }

    var email = "sakchyamthapa4@gmail.com";
    var existingUser = await userManager.FindByEmailAsync(email);

    if (existingUser != null)
    {
        Console.WriteLine("⚠️ Superadmin already exists - resetting password...");

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, "Test1234!");


        if (resetResult.Succeeded)
        {
            Console.WriteLine("✅ Password reset to Test1234!");
        }
        else
        {
            Console.WriteLine("❌ Failed to reset password:");
            foreach (var error in resetResult.Errors)
            {
                Console.WriteLine($"   • {error.Code}: {error.Description}");
            }
        }

        // ✅ Make sure they have the role too (just in case)
        if (!await userManager.IsInRoleAsync(existingUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(existingUser, "SuperAdmin");
            Console.WriteLine("✅ Superadmin role re-confirmed");
        }

        return;
    }

    // ✅ Create new user
    var superadmin = new User
    {
        UserName = "superadmin",
        Email = email,
        EmailConfirmed = true
    };

    var resultUser = await userManager.CreateAsync(superadmin, "Test1234!");
    if (!resultUser.Succeeded)
    {
        Console.WriteLine("❌ Error creating superadmin:");
        foreach (var e in resultUser.Errors)
        {
            Console.WriteLine($"   • {e.Code}: {e.Description}");
        }
        return;
    }

    var roleAssign = await userManager.AddToRoleAsync(superadmin, "SuperAdmin");
    Console.WriteLine($"✅ Superadmin role assigned: {roleAssign.Succeeded}");
    Console.WriteLine("🎉 Seeding complete");

}



if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SonicPoints API v1");
        c.RoutePrefix = string.Empty;
    });
}

await EnsureRolesAndSuperadminCreatedAsync(app);


app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

var redeemFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Redeemable Items");
if (!Directory.Exists(redeemFolderPath))
{
    Directory.CreateDirectory(redeemFolderPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(redeemFolderPath),
    RequestPath = "/Redeemable Items"
});

app.MapControllers();

Console.WriteLine(" SonicPoints API is running...");
app.Run();
