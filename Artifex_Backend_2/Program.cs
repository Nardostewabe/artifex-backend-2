using Artifex_Backend_2.Data;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services; // ✅ Ensure this is here
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// 1. Database
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ArtifexDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 29)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    ));

// ----------------------
// 2. Controllers (With Loop Fix)
// ----------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ✅ Stops the Login Crash (User -> Seller -> User loop)
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// ----------------------
// 3. Swagger
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// ----------------------
// 4. Auth & Password
// ----------------------
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();

var jwtSection = builder.Configuration.GetSection("Jwt");
// Use fallback keys if config is missing (prevents crashes)
var jwtKey = jwtSection.GetValue<string>("Key") ?? builder.Configuration["Jwt__Key"] ?? "TemporaryDefaultKeyForDevelopmentOnly";
var issuer = jwtSection.GetValue<string>("Issuer") ?? builder.Configuration["Jwt__Issuer"] ?? "Artifex";
var audience = jwtSection.GetValue<string>("Audience") ?? builder.Configuration["Jwt__Audience"] ?? "ArtifexUsers";

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
// 5. SERVICES (MISSING IN YOUR SNIPPET)
// ----------------------
// ⚠️ YOU MUST HAVE THESE OR THE APP WILL CRASH
builder.Services.AddScoped<IEmailService, EmailService>();
// If you have Chapa/Invoice services, uncomment these:
// builder.Services.AddHttpClient<IChapaService, ChapaService>();
// builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// ----------------------
// 6. CORS
// ----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ----------------------
// 7. Pipeline
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

// Render Port Setup
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();