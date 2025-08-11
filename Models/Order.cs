using System.ComponentModel.DataAnnotations;

namespace _1.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = "";

        public DateTime Created_Date { get; set; } = DateTime.Now;

        public int Status { get; set; }  // 1 đến 6

        public Customer? Customer { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
    }
}
