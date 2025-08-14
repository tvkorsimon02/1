using _1.Data;
using _1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace _1.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("username") == "admin";
        }

        private IActionResult RequireAdmin()
        {
            return IsAdmin() ? null! : RedirectToAction("Login", "Account");
        }

        // ------------------- Dashboard -------------------

        public IActionResult Index()
        {
            ViewBag.OrderCount = _context.Orders.Count(); // hoặc OrderDetails.Count()
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            return View();
        }

        // ------------------- CATEGORY -------------------

        //public IActionResult Category()
        //{
        //    // Active ở trên, sau đó sắp xếp theo tên
        //    var list = _context.Categories
        //        .OrderByDescending(c => c.Active)
        //        .ThenBy(c => c.Name)
        //        .ToList();

        //    return View(list);
        //}
        // /Admin/Categories (nếu bạn có view này)
        public IActionResult Categories(string sort = "status_desc")
        {
            var username = HttpContext.Session.GetString("username");
            if (username != "admin")
                return RedirectToAction("Login", "Account");

            ViewBag.Sort = sort;

            var q = _context.Categories.AsQueryable();
            q = sort == "status_asc"
                ? q.OrderBy(c => c.Active).ThenBy(c => c.Name)
                : q.OrderByDescending(c => c.Active).ThenBy(c => c.Name);

            var categories = q.ToList();
            return View(categories);
        }

        public IActionResult EditCategory(int id)
        {
            if (HttpContext.Session.GetString("username") != "admin")
                return RedirectToAction("Login", "Account");

            Category model = id == 0
                ? new Category() // thêm mới
                : _context.Categories.FirstOrDefault(c => c.Id == id); // sửa

            if (model == null)
                return NotFound();

            return View(model);
        }


        [HttpPost]
        public IActionResult EditCategory(Category model)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Categories"); // hoặc về lại danh sách
            }

            return View(model);
        }


        public IActionResult DeleteCategory(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var cat = _context.Categories.Find(id);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                _context.SaveChanges();
            }
            return RedirectToAction("Category");
        }
        [HttpPost]
        public IActionResult ToggleCategoryStatus(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var cat = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null) return NotFound();

            // Đảo trạng thái danh mục
            cat.Active = !cat.Active;

            // Áp trạng thái danh mục xuống tất cả sản phẩm cùng danh mục
            var products = _context.Products.Where(p => p.Category == id).ToList();
            foreach (var p in products)
                p.Active = cat.Active;  // Ẩn danh mục -> ẩn hết; Mở danh mục -> mở hết

            _context.SaveChanges();

            TempData["Message"] = cat.Active
                ? "Đã mở danh mục và hiển thị lại toàn bộ sản phẩm trong danh mục."
                : "Đã ẩn danh mục và ẩn toàn bộ sản phẩm trong danh mục.";

            return RedirectToAction("Category");
        }



        // ------------------- PRODUCT -------------------

        public IActionResult Product(string sort = "status_desc")
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            ViewBag.Sort = sort;

            var q = _context.Products
                .Include(p => p.CategoryNav)
                .AsQueryable();

            // status_desc: Active=true lên đầu | status_asc: Active=false lên đầu
            q = sort == "status_asc"
                ? q.OrderBy(p => p.Active).ThenBy(p => p.Name)
                : q.OrderByDescending(p => p.Active).ThenBy(p => p.Name);

            var list = q.ToList();
            return View(list);
        }


        [HttpPost]
        public IActionResult ToggleProductActive(int id)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var product = _context.Products
                            .Include(p => p.CategoryNav)
                            .FirstOrDefault(p => p.Id == id);

            if (product == null) return NotFound();

            if (product.CategoryNav != null && !product.CategoryNav.Active)
            {
                TempData["Message"] = "Không thể thay đổi trạng thái: Danh mục của sản phẩm đang ẩn.";
                return RedirectToAction("Product");
            }

            product.Active = !product.Active;
            _context.SaveChanges();

            TempData["Message"] = "Đã cập nhật trạng thái sản phẩm.";
            return RedirectToAction("Product");
        }
        

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            ViewBag.Categories = _context.Categories.Where(c => c.Active).ToList();

            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                // Nếu id = 0 thì là thêm mới
                return View(new Product());
            }
            return View(product);
        }


        [HttpPost]
        public IActionResult EditProduct(Product model, IFormFile ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
                model.ImageUrl = fileName;
            }

            if (model.Id == 0)
                _context.Products.Add(model);
            else
                _context.Products.Update(model);

            _context.SaveChanges();
            return RedirectToAction("Product"); // hoặc "Products"
        }


        // Nếu còn action DeleteProduct đâu đó:
        public IActionResult DeleteProduct(int id)
        {
            // KHÔNG xóa khi có lịch sử mua
            bool hasHistory = _context.OrderDetails.Any(od => od.Product == id);
            if (hasHistory)
            {
                TempData["Message"] = "Sản phẩm đã có trong đơn hàng, không thể xóa. Hãy ẨN thay vì xóa.";
                return RedirectToAction("Product");
            }

            var p = _context.Products.Find(id);
            if (p != null)
            {
                _context.Products.Remove(p);
                _context.SaveChanges();
            }
            return RedirectToAction("Product");
        }


        // ------------------- CUSTOMER -------------------

        public IActionResult Customers()
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            return View(_context.Customers.ToList());
        }

        [HttpPost]
        public IActionResult ToggleCustomerActive(string username)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var user = _context.Customers.FirstOrDefault(x => x.Username == username);
            if (user != null)
            {
                user.Active = !user.Active;
                _context.SaveChanges();
            }
            return RedirectToAction("Customers");
        }

        // ------------------- STATISTICS -------------------

        public IActionResult StatisticsSuccess(DateTime from, DateTime to)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var orders = _context.Orders
                .Where(o => o.Status == 4 && o.Created_Date >= from && o.Created_Date <= to)
                .ToList();
            return View("Stats", orders);
        }

        public IActionResult StatisticsCanceled(DateTime from, DateTime to)
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            var orders = _context.Orders
                .Where(o => o.Status == 6 && o.Created_Date >= from && o.Created_Date <= to)
                .ToList();
            return View("Stats", orders);
        }
        public IActionResult OrderDetails(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductNav) // <-- vẫn lấy được dù Product.Active = false
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }


        [HttpPost]
        public IActionResult UpdateStatus(int id, int status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = status;
            _context.SaveChanges();

            TempData["Message"] = "Cập nhật trạng thái thành công.";
            return RedirectToAction("OrderDetails", new { id });
        }
        // GET: /Admin/Orders
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductNav)
                .OrderByDescending(o => o.Created_Date)
                .ToList();

            return View(orders);
        }
        // POST: Cập nhật trạng thái
        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, int status)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductNav)
                .FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            var prev = order.Status;
            order.Status = status;

            // nếu chuyển sang "đã huỷ" (6) và trước đó không phải 6 -> trả kho
            if (status == 6 && prev != 6)
            {
                foreach (var d in order.OrderDetails!)
                {
                    if (d.ProductNav != null)
                        d.ProductNav.Quantity += d.Quantity;
                }
            }

            _context.SaveChanges();

            // Ajax: trả về json
            return Json(new { success = true, newStatus = status });
        }

        [HttpGet]
        public IActionResult Stats(DateTime? fromDate, DateTime? toDate)
        {
            // Bind lại lên ô chọn ngày (có thể null)
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var successOrders = new List<Order>();
            var cancelledOrders = new List<Order>();

            // Chỉ lọc khi đủ 2 ngày
            if (fromDate.HasValue && toDate.HasValue)
            {
                var start = fromDate.Value.Date;
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1); // cuối ngày

                var orders = _context.Orders
                    .Where(o => o.Created_Date >= start && o.Created_Date <= end)
                    .ToList();

                // NHỚ: đồng bộ mã status thành công theo DB của bạn (4 hoặc 5). Ở đây dùng 5.
                successOrders = orders.Where(o => o.Status == 5).ToList();
                cancelledOrders = orders.Where(o => o.Status == 6).ToList();

                // Tạo dải ngày liên tục để biểu đồ không khuyết cột
                var days = new List<DateTime>();
                for (var d = start.Date; d <= end.Date; d = d.AddDays(1)) days.Add(d);

                var sMap = successOrders.GroupBy(o => o.Created_Date.Date).ToDictionary(g => g.Key, g => g.Count());
                var cMap = cancelledOrders.GroupBy(o => o.Created_Date.Date).ToDictionary(g => g.Key, g => g.Count());

                var labels = days.Select(d => d.ToString("dd/MM")).ToList();
                var successSerie = days.Select(d => sMap.TryGetValue(d, out var v) ? v : 0).ToList();
                var cancelSerie = days.Select(d => cMap.TryGetValue(d, out var v) ? v : 0).ToList();

                // Đẩy JSON cho View (xài chung cho cả 2 tab)
                ViewBag.ChartLabels = JsonSerializer.Serialize(labels);
                ViewBag.SuccessSeries = JsonSerializer.Serialize(successSerie);
                ViewBag.CancelSeries = JsonSerializer.Serialize(cancelSerie);
            }
            else if (fromDate.HasValue ^ toDate.HasValue)
            {
                TempData["Message"] = "Vui lòng chọn cả Từ ngày và Đến ngày để xem thống kê.";
            }

            ViewBag.SuccessOrders = successOrders;
            ViewBag.CancelledOrders = cancelledOrders;

            return View();
        }
        // /Admin/Category
        // ------------------- CATEGORY -------------------
        public IActionResult Category(string sort = "status_desc")
        {
            var redirect = RequireAdmin();
            if (redirect != null) return redirect;

            ViewBag.Sort = sort;

            var q = _context.Categories.AsQueryable();

            // status_desc: Active (true) lên trên (mặc định)
            // status_asc : Inactive (false) lên trên
            q = sort == "status_asc"
                ? q.OrderBy(c => c.Active).ThenBy(c => c.Name)
                : q.OrderByDescending(c => c.Active).ThenBy(c => c.Name);

            var list = q.ToList();
            return View(list); // View: Views/Admin/Category.cshtml
        }
        [HttpPost]
        public IActionResult ToggleProductStatus(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                product.Active = !product.Active;
                _context.SaveChanges();
                TempData["Message"] = "Đã cập nhật trạng thái sản phẩm.";
            }
            return RedirectToAction("Product");
        }

    }
}
