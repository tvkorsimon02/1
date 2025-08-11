using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using _1.Models;
using _1.Data;
using Microsoft.EntityFrameworkCore;

namespace _1.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var products = _context.Products.Where(p => p.Active).ToList();
        return View(products);
    }

    public IActionResult Details(int id)
    {
        var product = _context.Products
            .Include(p => p.CategoryNav)
            .FirstOrDefault(p => p.Id == id && p.Active);

        if (product == null)
            return NotFound();

        return View(product);
    }
    // Hiển thị sản phẩm theo danh mục
    public IActionResult ByCategory(int id)
    {
        var category = _context.Categories
            .FirstOrDefault(c => c.Id == id && c.Active);

        if (category == null)
        {
            TempData["Message"] = "Danh mục không tồn tại hoặc đã bị ẩn.";
            return RedirectToAction("Index", "Home");
        }

        var products = _context.Products
            .Include(p => p.Category);
            

        ViewBag.CategoryName = category.Name;
        return View(products);
    }
}
