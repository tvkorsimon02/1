using System.ComponentModel.DataAnnotations;

namespace _1.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public bool Active { get; set; }

        public List<Product>? Products { get; set; }
    }
}
