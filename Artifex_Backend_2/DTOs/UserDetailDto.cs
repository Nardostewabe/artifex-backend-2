namespace Artifex_Backend_2.DTOs
{
    public class AdminUserDetailDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; } // Active, Suspended
        public DateTime JoinedDate { get; set; }

        // Detailed History
        public int OrderCount { get; set; }
        public List<SimpleOrderDto> RecentOrders { get; set; }
        public int ReportCount { get; set; } // How many times they've been reported
    }

    public class SimpleOrderDto
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}