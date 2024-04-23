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
    public class ColorsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ColorsController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Colors
        public async Task<IActionResult> Index()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(await _context.Colors.ToListAsync());
        }

        // GET: Colors/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Colors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Color color)
        {
            if (ModelState.IsValid)
            {
                _context.Add(color);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(color);
        }

        // GET: Colors/Edit/5
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

            var color = await _context.Colors.FindAsync(id);
            if (color == null)
            {
                return NotFound();
            }
            return View(color);
        }

        // POST: Colors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Color color)
        {
            if (id != color.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(color);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ColorExists(color.Id))
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
            return View(color);
        }

        // GET: Colors/Delete/5
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

            var color = await _context.Colors
                .FirstOrDefaultAsync(m => m.Id == id);
            if (color == null)
            {
                return NotFound();
            }

            return View(color);
        }

        // POST: Colors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var color = await _context.Colors.FindAsync(id);

            var products = await _context.Products.Where(p => p.ColorId == id).ToListAsync();

            if (color != null)
            {
                _context.Colors.Remove(color);
            }

            if (products != null)
            {
                _context.Products.RemoveRange(products);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ColorExists(int id)
        {
            return _context.Colors.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetString("Role") == "Admin";
        }
    }
}
