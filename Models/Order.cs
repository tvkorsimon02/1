using System.ComponentModel.DataAnnotations;

namespace _1.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        public DateTime Created_Date { get; set; } = DateTime.Now;

        public int Status { get; set; }  // 1..6

        // === Shipping info ===
        [Required, MaxLength(100)]
        public string ReceiverName { get; set; } = "";      // Lấy = Username

        [Required, MaxLength(20)]
        public string ReceiverPhone { get; set; } = "";

        [Required, MaxLength(255)]
        public string ShippingAddress { get; set; } = "";

        [MaxLength(500)]
        public string? Note { get; set; }

        public Customer? Customer { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
    }
}
