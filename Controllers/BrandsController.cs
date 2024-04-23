using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportFlex.Data;
using SportFlex.Models;

namespace SportFlex.Controllers
{
    public class BrandsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BrandsController(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var sessionId = _httpContextAccessor.HttpContext.Session.GetInt32("Id");
                var sessionLogin = _httpContextAccessor.HttpContext.Session.GetString("Login");
                var sessionRole = _httpContextAccessor.HttpContext.Session.GetString("Role");
                var sessionPassword = _httpContextAccessor.HttpContext.Session.GetString("Password");

                if (!string.IsNullOrEmpty(sessionLogin) &&
                    !string.IsNullOrEmpty(sessionPassword) &&
                    !string.IsNullOrEmpty(sessionRole) &&
                    sessionId.HasValue)
                {
                    var existingUser = _context.Users.FirstOrDefault(
                        u => u.Login == sessionLogin &&
                        u.Password == sessionPassword &&
                        u.Id == sessionId);

                    if (existingUser != null)
                    {
                        ViewBag.Login = sessionLogin;
                        ViewBag.Role = sessionRole;
                    }
                    else
                    {
                        _httpContextAccessor.HttpContext.Session.Remove("Id");
                        _httpContextAccessor.HttpContext.Session.Remove("Login");
                        _httpContextAccessor.HttpContext.Session.Remove("Role");
                        _httpContextAccessor.HttpContext.Session.Remove("Password");
                        ViewBag.Login = null;
                        ViewBag.Role = null;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        // GET: Brands
        public async Task<IActionResult> Index()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(await _context.Brands.ToListAsync());
        }

        // GET: Brands/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Brands/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brands/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brands/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            var products = await _context.Products.Where(p => p.BrandId == id).ToListAsync();

            if (brand != null)
            {
                _context.Brands.Remove(brand);
            }

            if (products != null)
            {
                _context.Products.RemoveRange(products);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetString("Role") == "Admin";
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
