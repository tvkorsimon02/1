using _1.Data;
using _1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.Carts
                .Where(c => c.Username == username)
                .ToList();

            // Gán thủ công ProductNav
            foreach (var item in cartItems)
            {
                item.ProductNav = _context.Products.FirstOrDefault(p => p.Id == item.Product);
            }

            return View(cartItems);
        }


        public IActionResult Add(int id)
        {
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ.";
                return RedirectToAction("Login", "Account");
            }

            // Tìm cart theo Username + Product (vì bạn không dùng CustomerId/ProductId trong model)
            var existing = _context.Carts.FirstOrDefault(c => c.Username == username && c.Product == id);

            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    Username = username,
                    Product = id,
                    Quantity = 1
                });
            }

            _context.SaveChanges();

            // Tính tổng số lượng giỏ hàng theo Username
            var cartCount = _context.Carts
                .Where(c => c.Username == username)
                .Sum(c => c.Quantity);

            HttpContext.Session.SetInt32("cartCount", cartCount);

            return RedirectToAction("Index", "Cart");
        }
        [HttpPost]
        public IActionResult Update(List<Cart> CartItems)
        {
            foreach (var item in CartItems)
            {
                var existing = _context.Carts.FirstOrDefault(c => c.Id == item.Id);
                if (existing != null)
                {
                    existing.Quantity = item.Quantity;
                }
            }

            _context.SaveChanges();

            // Cập nhật số lượng trong session
            var username = HttpContext.Session.GetString("username");
            var totalCount = _context.Carts
                .Where(c => c.Username == username)
                .Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("cartCount", totalCount);

            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteSelected(List<int> SelectedIds)
        {
            if (SelectedIds != null && SelectedIds.Any())
            {
                var items = _context.Carts.Where(c => SelectedIds.Contains(c.Id)).ToList();
                _context.Carts.RemoveRange(items);
                _context.SaveChanges();
            }

            // Cập nhật lại số lượng giỏ
            var username = HttpContext.Session.GetString("username");
            var totalCount = _context.Carts
                .Where(c => c.Username == username)
                .Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("cartCount", totalCount);

            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Checkout(List<int> selectedIds, List<Cart> CartItems)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            // 1) Cập nhật số lượng từ form (nếu có)
            if (CartItems != null)
            {
                foreach (var ci in CartItems)
                {
                    var existing = _context.Carts.FirstOrDefault(c => c.Id == ci.Id && c.Username == username);
                    if (existing != null && ci.Quantity > 0)
                        existing.Quantity = ci.Quantity;
                }
                _context.SaveChanges();
            }

            // 2) Lấy các item được chọn để tạo đơn
            var selectedCarts = _context.Carts
                .Include(c => c.ProductNav)
                .Where(c => selectedIds.Contains(c.Id) && c.Username == username)
                .ToList();

            if (!selectedCarts.Any())
            {
                TempData["Message"] = "Vui lòng chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index");
            }

            var order = new Order
            {
                Created_Date = DateTime.Now,
                Username = username,
                Status = 1,
                OrderDetails = selectedCarts.Select(c => new OrderDetail
                {
                    Product = c.Product,
                    Quantity = c.Quantity,
                    Price = c.ProductNav?.Price ?? 0
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.Carts.RemoveRange(selectedCarts);
            _context.SaveChanges();

            // cập nhật lại badge số lượng giỏ hàng
            var totalCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("cartCount", totalCount);

            return RedirectToAction("Confirm", "Orders", new { id = order.Id });
        }


        public IActionResult Confirm(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }


    }
}
