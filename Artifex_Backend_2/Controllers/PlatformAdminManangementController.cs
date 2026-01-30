using Artifex_Backend_2.Data;
using Artifex_Backend_2.DTOs;
using Artifex_Backend_2.Models;
using Artifex_Backend_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // ✅ Needed for IPasswordHasher
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Artifex_Backend_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "4")] // Platform Admin Only
    public class PlatformAdminManagementController : ControllerBase
    {
        private readonly ArtifexDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher; // ✅ Inject Standard Hasher
        private readonly IEmailService _emailService;

        public PlatformAdminManagementController(ArtifexDbContext db, IPasswordHasher<User> passwordHasher, IEmailService emailService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        // [POST] Create a new Content Admin
        [HttpPost("create-content-admin")]
        public async Task<IActionResult> CreateContentAdmin([FromBody] CreateAdminDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("User with this email already exists.");

            var newUser = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = UserRole.ContentAdmin,
                CreatedAt = DateTime.UtcNow
            };

            // ✅ USE STANDARD HASHING (Matches AuthController)
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, dto.Password);

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            var contentAdmin = new ContentAdmin
            {
                UserId = newUser.Id,
                FullName = dto.FullName,
                Department = dto.Department
            };
            _db.ContentAdmins.Add(contentAdmin);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Content Admin created successfully." });
        }

        // [GET] List all Content Admins
        [HttpGet("list-content-admins")]
        public async Task<IActionResult> GetAllContentAdmins()
        {
            var admins = await _db.ContentAdmins
                .Include(c => c.User)
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Department,
                    Email = c.User.Email,
                    JoinedDate = c.AssignedAt
                })
                .ToListAsync();

            return Ok(admins);
        }

        // [DELETE] Fire a Content Admin
        [HttpDelete("remove-admin/{id}")]
        public async Task<IActionResult> RemoveContentAdmin(Guid id)
        {
            var admin = await _db.ContentAdmins.FindAsync(id);
            if (admin == null) return NotFound("Admin not found.");

            var user = await _db.Users.FindAsync(admin.UserId);

            _db.ContentAdmins.Remove(admin);
            if (user != null) _db.Users.Remove(user);

            await _db.SaveChangesAsync();
            return Ok("Content Admin removed.");
        }

        [HttpPost("suspend-user/{userId}")]
        public async Task<IActionResult> SuspendUser(Guid userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (user.Role == UserRole.PlatformAdmin)
                return BadRequest("You cannot suspend another Platform Admin.");

            // ✅ Toggle Status
            user.IsSuspended = !user.IsSuspended;

            // ✅ Prepare Email
            string subject, body;

            if (user.IsSuspended)
            {
                subject = "Account Suspended - Artifex";
                body = $@"
                    <h3 style='color: red;'>Account Suspended</h3>
                    <p>Hello {user.Username},</p>
                    <p>Your account has been suspended due to violations of our terms of service.</p>
                    <p>You will no longer be able to log in or make purchases/sales.</p>
                    <p>If you believe this is an error, please contact support.</p>";
            }
            else
            {
                subject = "Account Restored - Artifex";
                body = $@"
                    <h3 style='color: green;'>Account Restored</h3>
                    <p>Hello {user.Username},</p>
                    <p>Good news! Your account suspension has been lifted.</p>
                    <p>You may now log in and use Artifex normally.</p>";
            }

            // Send Email
            await _emailService.SendEmailAsync(user.Email, subject, body);

            await _db.SaveChangesAsync();

            string statusMsg = user.IsSuspended ? "suspended" : "restored";
            return Ok($"User {user.Username} has been {statusMsg}.");
        }
    }
}