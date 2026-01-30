namespace Artifex_Backend_2.DTOs
{
    public class InitializePaymentDto
    {
        public decimal Amount { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ChapaResponseDto
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public ChapaData Data { get; set; }
    }

    public class ChapaData
    {
        public string Checkout_Url { get; set; }
    }
}