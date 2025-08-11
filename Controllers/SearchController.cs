using _1.Data;
using _1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _1.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;
        public SearchController(AppDbContext context) { _context = context; }

        // /Search?query=...&categoryId=...&min=...&max=...&sort=...&page=1
        public IActionResult Index(string? query, int? categoryId, long? min, long? max, string? sort, int page = 1, int pageSize = 12)
        {
            // Chuẩn bị danh mục cho filter
            ViewBag.Categories = _context.Categories.Where(c => c.Active).OrderBy(c => c.Name).ToList();

            // Base query
            var q = _context.Products
                    .Include(p => p.CategoryNav)
                    .Where(p => p.Active && p.CategoryNav!.Active)
                    .AsQueryable();

            // Lọc theo từ khoá (không phân biệt hoa/thường)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var kw = query.Trim();
                q = q.Where(p =>
                    EF.Functions.Like(p.Name, $"%{kw}%")
                // Nếu bạn có mô tả: || EF.Functions.Like(p.Description, $"%{kw}%")
                );
            }

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
                q = q.Where(p => p.Category == categoryId.Value);

            // Lọc theo giá
            if (min.HasValue) q = q.Where(p => p.Price >= min.Value);
            if (max.HasValue) q = q.Where(p => p.Price <= max.Value);

            // Sắp xếp
            q = sort switch
            {
                "price_asc" => q.OrderBy(p => p.Price),
                "price_desc" => q.OrderByDescending(p => p.Price),
                "name_asc" => q.OrderBy(p => p.Name),
                "name_desc" => q.OrderByDescending(p => p.Name),
                _ => q.OrderByDescending(p => p.Id) // mới nhất
            };

            // Phân trang
            var total = q.Count();
            var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ViewBag để bind lại form
            ViewBag.Query = query;
            ViewBag.CategoryId = categoryId;
            ViewBag.Min = min;
            ViewBag.Max = max;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;

            return View(items); // trả về List<Product>
        }

        // Gợi ý nhanh (autocomplete) -> trả JSON 5 sản phẩm
        [HttpGet]
        public IActionResult Suggest(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new string[] { });

            var data = _context.Products
                .Where(p => p.Active && EF.Functions.Like(p.Name, $"%{term.Trim()}%"))
                .OrderBy(p => p.Name)
                .Take(5)
                .Select(p => new { id = p.Id, name = p.Name })
                .ToList();

            return Json(data);
        }
    }
}
