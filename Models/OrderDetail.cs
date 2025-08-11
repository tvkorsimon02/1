using System.ComponentModel.DataAnnotations.Schema;

namespace _1.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("ProductNav")]
        public int Product { get; set; }

        public long Price { get; set; }

        public int Quantity { get; set; }

        public Order? Order { get; set; }
        public Product? ProductNav { get; set; }
    }
}
