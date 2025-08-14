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

            // Gán ProductNav thủ công
            foreach (var item in cartItems)
            {
                item.ProductNav = _context.Products.FirstOrDefault(p => p.Id == item.Product);
            }

            return View(cartItems);
        }

        // Thêm 1 sản phẩm vào giỏ (không cho vượt tồn)
        public IActionResult Add(int id)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["Message"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ.";
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == id && p.Active);
            if (product == null)
            {
                TempData["Message"] = "Sản phẩm không tồn tại hoặc đã bị ẩn.";
                return RedirectToAction("Index", "Home");
            }

            var existing = _context.Carts.FirstOrDefault(c => c.Username == username && c.Product == id);
            var newQty = (existing?.Quantity ?? 0) + 1;

            if (newQty > product.Quantity)
            {
                TempData["Message"] = $"Chỉ còn {product.Quantity} \"{product.Name}\" trong kho.";
                return RedirectToAction("Index", "Home");
            }

            if (existing != null)
            {
                existing.Quantity = newQty;
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

            // cập nhật badge giỏ hàng
            var cartCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("cartCount", cartCount);

            return RedirectToAction("Index", "Cart");
        }

        // Cập nhật nhiều item trong giỏ (clamp về tối đa tồn kho)
        [HttpPost]
        public IActionResult Update(List<Cart> CartItems)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            if (CartItems != null)
            {
                foreach (var item in CartItems)
                {
                    var existing = _context.Carts.FirstOrDefault(c => c.Id == item.Id && c.Username == username);
                    if (existing == null) continue;

                    var product = _context.Products.FirstOrDefault(p => p.Id == existing.Product && p.Active);
                    if (product == null) continue;

                    var desired = item.Quantity < 1 ? 1 : item.Quantity;
                    if (desired > product.Quantity)
                    {
                        existing.Quantity = product.Quantity; // clamp
                        TempData["Message"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.Quantity} trong kho.";
                    }
                    else
                    {
                        existing.Quantity = desired;
                    }
                }
                _context.SaveChanges();
            }

            // Cập nhật badge
            var totalCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
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

            var username = HttpContext.Session.GetString("username");
            var totalCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
            HttpContext.Session.SetInt32("cartCount", totalCount);

            return RedirectToAction("Index");
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
        // XÓA 1 MẶT HÀNG (nếu chưa có)
        [HttpPost]
        public IActionResult DeleteSingle(int id)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            var item = _context.Carts.FirstOrDefault(c => c.Id == id && c.Username == username);
            if (item != null)
            {
                _context.Carts.Remove(item);
                _context.SaveChanges();

                var totalCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
                HttpContext.Session.SetInt32("cartCount", totalCount);
                TempData["Message"] = "Đã xoá sản phẩm khỏi giỏ hàng.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        // [ValidateAntiForgeryToken] // bật nếu form có AntiForgeryToken
        public IActionResult Checkout(
    List<int> selectedIds,
    List<Cart> CartItems,
    string receiverPhone,
    string shippingAddress,
    string note
)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            // cập nhật số lượng theo form (nếu có)
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

            // lấy item được chọn
            var selectedCarts = _context.Carts
                .Include(c => c.ProductNav)
                .Where(c => selectedIds.Contains(c.Id) && c.Username == username)
                .ToList();

            if (!selectedCarts.Any())
            {
                TempData["Message"] = "Vui lòng chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index");
            }

            // chặn sản phẩm đã ẩn
            var inactive = selectedCarts.Where(c => c.ProductNav == null || !c.ProductNav.Active).ToList();
            if (inactive.Any())
            {
                TempData["Message"] = "Một số sản phẩm đã ngừng bán, vui lòng bỏ chọn hoặc xoá khỏi giỏ.";
                return RedirectToAction("Index");
            }

            // kiểm tra tồn kho
            var lack = selectedCarts
                .Where(c => (c.ProductNav?.Quantity ?? 0) < c.Quantity)
                .Select(c => $"{c.ProductNav!.Name} (còn {(c.ProductNav!.Quantity):N0}, yêu cầu {c.Quantity:N0})")
                .ToList();

            if (lack.Any())
            {
                TempData["Message"] = "Số lượng không đủ: " + string.Join("; ", lack);
                return RedirectToAction("Index");
            }

            // tối thiểu hoá validate input giao hàng (tuỳ bạn siết thêm)
            receiverPhone = (receiverPhone ?? "").Trim();
            shippingAddress = (shippingAddress ?? "").Trim();
            note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            if (string.IsNullOrWhiteSpace(receiverPhone) || string.IsNullOrWhiteSpace(shippingAddress))
            {
                TempData["Message"] = "Vui lòng nhập số điện thoại và địa chỉ nhận hàng.";
                return RedirectToAction("Index");
            }

            using var tx = _context.Database.BeginTransaction();
            try
            {
                var order = new Order
                {
                    Created_Date = DateTime.Now,
                    Username = username,
                    Status = 1,                          // đã đặt
                    ReceiverName = username,             // theo yêu cầu
                    ReceiverPhone = receiverPhone,
                    ShippingAddress = shippingAddress,
                    Note = note,
                    OrderDetails = selectedCarts.Select(c => new OrderDetail
                    {
                        Product = c.Product,
                        Quantity = c.Quantity,
                        Price = c.ProductNav!.Price
                    }).ToList()
                };

                _context.Orders.Add(order);

                // trừ tồn
                foreach (var c in selectedCarts)
                {
                    c.ProductNav!.Quantity -= c.Quantity;
                }

                // xoá các item đã thanh toán khỏi giỏ
                _context.Carts.RemoveRange(selectedCarts);

                _context.SaveChanges();
                tx.Commit();

                // cập nhật badge giỏ
                var totalCount = _context.Carts.Where(c => c.Username == username).Sum(c => c.Quantity);
                HttpContext.Session.SetInt32("cartCount", totalCount);

                return RedirectToAction("Confirm", "Orders", new { id = order.Id });
            }
            catch
            {
                tx.Rollback();
                TempData["Message"] = "Có lỗi khi tạo đơn. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

    }
}
