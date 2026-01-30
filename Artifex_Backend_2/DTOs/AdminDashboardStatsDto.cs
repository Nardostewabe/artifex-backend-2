namespace Artifex_Backend_2.DTOs
{
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingDisputes { get; set; }
        public List<ChartDataDto> NewUsersLast7Days { get; set; }
    }

    public class ChartDataDto
    {
        public string Date { get; set; }
        public int Count { get; set; }
    }
}