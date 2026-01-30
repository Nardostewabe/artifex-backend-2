namespace Artifex_Backend_2.DTOs
{
    public class DisputeDto
    {
        public int Id { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string ComplainantName { get; set; } // Flattened
        public int? OrderId { get; set; }
        public string CreatedAt { get; set; }
    }
}