using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _1.Models;
using _1.Data;

namespace _1.Controllers
{
    public class CustomersController : Controller
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách khách hàng
        public IActionResult Index()
        {
            var customers = _context.Customers.ToList();
            return View(customers);
        }

        // Edit
        public IActionResult Edit(string username)
        {
            if (username == null) return NotFound();

            var customer = _context.Customers.Find(username);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(customer).State = EntityState.Modified;
                _context.SaveChanges();
                TempData["Message"] = "Cập nhật thông tin khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
    }
}
