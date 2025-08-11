using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _1.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string Username { get; set; } = "";

        public int Product { get; set; }

        public int Quantity { get; set; }

        public Customer? Customer { get; set; }
        [ForeignKey("Product")]
        public Product? ProductNav { get; set; }

    }
}
