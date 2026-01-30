using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services; // ✅ Ensure this is here for IEmailService
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Artifex_Backend_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService; // ✅ Added missing field

        // ✅ Updated Constructor to accept IEmailService
        public AuthController(ArtifexDbContext db, IPasswordHasher<User> hasher, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
            _emailService = emailService;
        }

        private string GenerateJwtToken(User user)
        {
            // Robust key retrieval for both Local and Render
            var secretKey = _config["Jwt:Key"] ?? _config["Jwt__Key"];
            var issuer = _config["Jwt:Issuer"] ?? _config["Jwt__Issuer"];
            var audience = _config["Jwt:Audience"] ?? _config["Jwt__Audience"];

            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, ((int)user.Role).ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ---------------- Signup ----------------

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest("username, email and password are required.");
            }

            if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
                return Conflict("username already exists.");

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("email already registered.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = UserRole.User // default role before profile setup
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            // Return safe object to avoid loops
            return Ok(new
            {
                message = "Signup successful.",
                token = token,
                user = new { user.Id, user.Username, user.Email, user.Role }
            });
        }

        // ---------------- Login ---------------

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Username/email and password are required.");
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null)
                return Unauthorized("User not found.");

            // Check Seller Approval
            bool isApproved = false;
            if (user.Role == UserRole.Seller)
            {
                var sellerProfile = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (sellerProfile != null)
                {
                    isApproved = sellerProfile.IsApproved;
                }
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid password.");

            var token = GenerateJwtToken(user);

            // ✅ SAFE RETURN OBJECT (Prevents 500 Infinite Loop Error)
            return Ok(new
            {
                message = "Login successful.",
                token = token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    IsApproved = isApproved
                }
            });
        }

        // ---------------- Forgot Password ----------------

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return Ok(new { message = "If an account exists with this email, a reset link has been sent." });
            }

            // Generate Token
            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _db.SaveChangesAsync();

            // ✅ FIX: Use dynamic Frontend URL for Production vs Local
            var frontendUrl = "https://artifex-frontend.onrender.com"; // Default to Live

            // If testing locally, you can uncomment this line:
            // frontendUrl = "http://localhost:5173"; 

            var resetLink = $"{frontendUrl}/reset-password?token={token}";

            var emailBody = $@"
                <h1>Password Reset Request</h1>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>This link expires in 1 hour.</p>";

            // Now _emailService is properly injected and will work
            await _emailService.SendEmailAsync(user.Email, "Reset Your Password", emailBody);

            return Ok(new { message = "If an account exists with this email, a reset link has been sent." });
        }

        // ---------------- Reset Password ----------------

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired password reset token.");
            }

            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully. You can now login." });
        }
    }
}