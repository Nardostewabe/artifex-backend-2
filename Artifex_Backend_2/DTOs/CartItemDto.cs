namespace Artifex_Backend_2.DTOs
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
    }
}
