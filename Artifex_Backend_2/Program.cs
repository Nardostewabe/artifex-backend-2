using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 1. Configuration & Secrets Check
// ----------------------
// This pulls from appsettings.json in dev, or Environment Variables in production
var chapaKey = builder.Configuration["CHAPA_SECRET_KEY"];

if (string.IsNullOrEmpty(chapaKey)) 
{
    // Log a warning if the key is missing; crucial for debugging Render deployments
    Console.WriteLine("Warning: CHAPA_SECRET_KEY is missing! Payment features will fail.");
}

// ----------------------
// 2. Database (MySQL)
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ArtifexDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 29)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    ));

// ----------------------
// 3. Controllers & JSON Options
// ----------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevents circular reference errors when including Sellers/Categories
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// ----------------------
// 4. Swagger
// ----------------------
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ----------------------
// 5. Identity & JWT Authentication
// ----------------------
builder.Services.AddSingleton<
    Microsoft.AspNetCore.Identity.IPasswordHasher<User>,
    Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? "TemporaryDefaultKeyForDevelopmentOnly";
var issuer = jwtSection.GetValue<string>("Issuer") ?? "Artifex";
var audience = jwtSection.GetValue<string>("Audience") ?? "ArtifexUsers";

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ----------------------
// 6. Services & CORS
// ----------------------
builder.Services.AddScoped<Artifex_Backend_2.Services.IEmailService, Artifex_Backend_2.Services.EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ----------------------
// 7. Middleware Pipeline
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ----------------------
// 8. Render Port Binding Fix
// ----------------------
// Render injects a PORT variable; this ensures the app listens to it
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
