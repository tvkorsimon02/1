using System.ComponentModel.DataAnnotations.Schema;

namespace _1.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public long Price { get; set; }
        public int Quantity { get; set; } = 0;

        // Đây là khóa ngoại thực tế
        public int Category { get; set; }

        // Đây là navigation property, nên bạn cần chỉ định rõ khóa ngoại dùng cột nào
        [ForeignKey("Category")]
        public Category? CategoryNav { get; set; }

        public bool Active { get; set; } = true;
    }

}
