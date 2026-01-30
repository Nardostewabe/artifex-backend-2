namespace Artifex_Backend_2.DTOs
{
    public class CreateAdminDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; } // "Blog", "Video", etc.
    }
}