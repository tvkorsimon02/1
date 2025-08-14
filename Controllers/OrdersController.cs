using _1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var orders = _context.Orders.ToList();
            return View(orders);
        }

        [HttpGet]
        public IActionResult LoadByStatus(int status)
        {
            var query = _context.Orders.AsQueryable();
            if (status > 0)
                query = query.Where(o => o.Status == status);

            var result = query.ToList();
            return PartialView("_OrderTable", result);
        }
        public IActionResult MyOrders()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders
                .Where(o => o.Username == username)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductNav)
                .OrderByDescending(o => o.Created_Date)
                .ToList();

            return View(orders);
        }


        // Chi tiết đơn hàng cho khách hàng
        public IActionResult Details(int id)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            // Chỉ lấy đơn của chính khách hàng đang đăng nhập
            var order = _context.Orders
        .Include(o => o.OrderDetails)
            .ThenInclude(od => od.ProductNav)  // Load thông tin sản phẩm
        .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("MyOrders");
            }

            return View(order);
        }
        public IActionResult Confirm(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductNav)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View("~/Views/Cart/Confirm.cshtml", order);

        }
        [HttpPost]
        public IActionResult CancelOrder(int id)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductNav)
                .FirstOrDefault(o => o.Id == id && o.Username == username);

            if (order == null)
            {
                TempData["Message"] = "Đơn hàng không tồn tại!";
                return RedirectToAction("MyOrders");
            }

            if (order.Status == 1) // chỉ cho huỷ khi còn chờ duyệt
            {
                // trả kho
                foreach (var d in order.OrderDetails!)
                {
                    if (d.ProductNav != null)
                        d.ProductNav.Quantity += d.Quantity;
                }

                order.Status = 6; // đã huỷ
                _context.SaveChanges();
                TempData["Message"] = "Đơn hàng đã được hủy và số lượng đã trả lại kho.";
            }
            else
            {
                TempData["Message"] = "Đơn hàng không thể hủy vì đã được xử lý!";
            }

            return RedirectToAction("Details", new { id });
        }


    }

}
