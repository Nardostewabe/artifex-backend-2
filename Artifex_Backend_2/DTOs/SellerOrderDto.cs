namespace Artifex_Backend_2.DTOs
{
    public class SellerOrderDto
    {
        public int OrderId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } // "Pending", "Shipped", "Delivered"
        public DateTime OrderDate { get; set; }

        // Buyer Info
        public string BuyerName { get; set; }
        public string ShippingAddress { get; set; }

        // ✅ NEW: Add these to fix the CS0117 Error
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string NewStatus { get; set; }
    }
}