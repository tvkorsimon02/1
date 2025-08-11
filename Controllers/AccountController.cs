using _1.Data;
using _1.Models;
using Microsoft.AspNetCore.Mvc;

using _1.Data;
using _1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq; // Đảm bảo có dòng này

namespace _1.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // ✔ GÁN CỨNG admin
            if (username == "admin" && password == "123456")
            {
                HttpContext.Session.SetString("username", "admin");
                // Cần thêm Session để lưu tên admin nếu muốn hiển thị trên giao diện
                // HttpContext.Session.SetString("customerName", "Admin");
                return RedirectToAction("Index", "Admin");
            }

            // 👤 Khách hàng thường
            var user = _context.Customers
                .FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null && user.Active)
            {
                HttpContext.Session.SetString("username", user.Username);
                // Lưu tên người dùng vào Session để hiển thị trên header nếu cần
                HttpContext.Session.SetString("customerName", user.FullName); // Lưu cả tên khách hàng
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
            return View();
        }

        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string Username, string UsernameConfirm, string Password, string FullName, string Gender, DateTime BirthDate)
        {
            if (Username != UsernameConfirm)
            {
                TempData["Message"] = "Tên tài khoản nhập lại không khớp!";
                return View();
            }

            // Kiểm tra trùng username
            if (_context.Customers.Any(c => c.Username == Username))
            {
                TempData["Message"] = "Tên tài khoản đã tồn tại!";
                return View();
            }

            var customer = new Customer
            {
                Username = Username,
                Password = Password, // Hash password nếu cần
                FullName = FullName,
                Gender = Gender,
                BirthDate = BirthDate,
                Active = true
            };

            _context.Customers.Add(customer);
            _context.SaveChanges();

            TempData["Message"] = "Đăng ký thành công, vui lòng đăng nhập!";
            return RedirectToAction("Login");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            var customer = _context.Customers.FirstOrDefault(c => c.Username == username);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        [HttpPost]
        public IActionResult Profile(Customer model)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            var customer = _context.Customers.FirstOrDefault(c => c.Username == username);
            if (customer == null)
                return NotFound();

            customer.FullName = model.FullName;
            customer.Gender = model.Gender;
            customer.BirthDate = model.BirthDate;

            _context.SaveChanges();
            TempData["Message"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
    }
}
