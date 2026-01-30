namespace Artifex_Backend_2.DTOs
{
    public class CustomerProfileDto
    {
        public string FullName { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string? ShippingAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
