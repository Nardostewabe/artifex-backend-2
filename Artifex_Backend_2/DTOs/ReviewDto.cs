namespace Artifex_Backend_2.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } // The name of the reviewer
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Date { get; set; } // Formatted date string
    }
}