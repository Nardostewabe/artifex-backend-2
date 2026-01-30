using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IEmailService _emailService;

        public AuthController(ArtifexDbContext db, IPasswordHasher<User> hasher, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
            _emailService = emailService;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, ((int)user.Role).ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
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

            // 1. CRITICAL: Check for null FIRST.
            // If this is null, we must stop immediately.
            if (user == null)
                return Unauthorized("User not found.");

            // 2. Now it is safe to access user.Role
            bool isApproved = false;
            if (user.Role == UserRole.Seller)
            {
                var sellerProfile = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (sellerProfile != null)
                {
                    isApproved = sellerProfile.IsApproved;
                }
            }

            // ... continue with password check ...
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid password.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful.",
                token = token,
                user = new { user.Id, user.Username, user.Email, user.Role, IsApproved = isApproved }
            });
        }


    [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                // Security: Don't reveal if email exists or not. Just say "If email exists..."
                return Ok(new { message = "If an account exists with this email, a reset link has been sent." });
            }

            // Generate Token
            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Valid for 1 hour

            await _db.SaveChangesAsync();

            // Create Link (Frontend URL)
            // Assumes your React app runs on port 5173
            var resetLink = $"http://localhost:5173/reset-password?token={token}";

            // Send Email
            var emailBody = $@"
                <h1>Password Reset Request</h1>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>This link expires in 1 hour.</p>";

            await _emailService.SendEmailAsync(user.Email, "Reset Your Password", emailBody);

            return Ok(new { message = "If an account exists with this email, a reset link has been sent." });
        }

        // ---------------------------------------------------------
        // 2. Reset Password (Submit New Password)
        // ---------------------------------------------------------
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired password reset token.");
            }

            // Hash the new password
            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);

            // Clear the token so it can't be used again
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully. You can now login." });
        }
    }
}
