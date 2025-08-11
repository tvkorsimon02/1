using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _1.Models
{
    public class Customer
    {
        [Key]
        [MaxLength(50)]
        public string Username { get; set; } = "";

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = "";

        // Họ và tên đầy đủ
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        // Giới tính: Nam, Nữ, Khác...
        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? BirthDate { get; set; }

        public bool Active { get; set; }

        // Liên kết các bảng khác
        public List<Order>? Orders { get; set; }
        public List<Cart>? Carts { get; set; }
    }
}
