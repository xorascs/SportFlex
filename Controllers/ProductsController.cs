using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportFlex.Data;
using SportFlex.Models;

namespace SportFlex.Controllers
{
    public class ProductsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductsController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Products
        public async Task<IActionResult> Index(string searchString, string brandFilter, string categoryFilter, string colorFilter)
        {
            // Retrieve all products with their related entities
            IQueryable<Product> products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Color);

            // Apply search string filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            // Apply brand filter if provided
            if (!string.IsNullOrEmpty(brandFilter))
            {
                products = products.Where(p => p.Brand!.Name.Contains(brandFilter));
            }

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                products = products.Where(p => p.Category!.Name.Contains(categoryFilter));
            }

            // Apply color filter if provided
            if (!string.IsNullOrEmpty(colorFilter))
            {
                products = products.Where(p => p.Color!.Name.Contains(colorFilter));
            }

            // Populate ViewBag with brands, categories, and colors for dropdowns
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Colors = await _context.Colors.ToListAsync();

            // Return the filtered list of products to the view
            return View(await products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productViewModel = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Color)
                .Select(p => new ProductViewModel
                {
                    Product = p,
                    Reviews = _context.Reviews
                        .Include(r => r.User)
                        .Where(r => r.ProductId == p.Id)
                        .ToList()
                })
                .FirstOrDefaultAsync(m => m.Product!.Id == id);

            if (productViewModel == null)
            {
                return NotFound();
            }

            return View(productViewModel);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["ColorId"] = new SelectList(_context.Colors, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BrandId,ColorId,CategoryId,Name,Description,ImagePaths")] Product product, List<IFormFile> ImagePaths)
        {
            if (ModelState.IsValid)
            {
                // Check if any files were uploaded
                if (ImagePaths != null && ImagePaths.Count > 0)
                {
                    foreach (var file in ImagePaths)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

                            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                            if (!Directory.Exists(uploadsDirectory))
                            {
                                Directory.CreateDirectory(uploadsDirectory);
                            }

                            var filePath = Path.Combine(uploadsDirectory, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            product.ImagePaths.Add(uniqueFileName);
                        }
                    }
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["ColorId"] = new SelectList(_context.Colors, "Id", "Name", product.ColorId);
            return View(product);
        }

        // GET: Products/Edit/5
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

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["ColorId"] = new SelectList(_context.Colors, "Id", "Name", product.ColorId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BrandId,ColorId,CategoryId,Name,Description,ImagePaths")] Product product, List<IFormFile> ImagePaths)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _context.Products.FirstOrDefaultAsync(c => c.Id == product.Id);

                    if (ImagePaths != null && ImagePaths.Count > 0)
                    {
                        foreach (var imagePath in originalProduct!.ImagePaths)
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imagePath);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }

                        originalProduct.ImagePaths.Clear();

                        foreach (var file in ImagePaths)
                        {
                            if (file != null && file.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

                                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                                if (!Directory.Exists(uploadsDirectory))
                                {
                                    Directory.CreateDirectory(uploadsDirectory);
                                }

                                var filePath = Path.Combine(uploadsDirectory, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                }

                                originalProduct.ImagePaths.Add(uniqueFileName);
                            }
                        }
                    }

                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    originalProduct.BrandId = product.BrandId;
                    originalProduct.ColorId = product.ColorId;
                    originalProduct.CategoryId = product.CategoryId;
                    originalProduct.Name = product.Name;
                    originalProduct.Description = product.Description;

                    _context.Update(originalProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["ColorId"] = new SelectList(_context.Colors, "Id", "Name", product.ColorId);
            return View(product);
        }

        // GET: Products/Delete/5
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

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Color)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            var reviews = await _context.Reviews.Where(r => r.ProductId == id).ToListAsync();

            if (product != null)
            {
                if (product.ImagePaths != null && product.ImagePaths.Count > 0)
                {
                    // Remove images from directory
                    foreach (var imagePath in product.ImagePaths)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imagePath);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                _context.Products.Remove(product);
            }

            if (reviews != null)
            {
                _context.Reviews.RemoveRange(reviews);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SubmitComment(int id, string commentText)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Users");
            }

            var product = await _context.Products.FindAsync(id);
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (product == null || user == null)
            {
                return BadRequest();
            }

            var addComment = new Review
            {
                UserId = user.Id,
                ProductId = product.Id,
                Comment = commentText,
                CreatedAt = DateTime.Now,
            };

            _context.Reviews.Add(addComment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = addComment.ProductId });
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetString("Role") == "Admin";
        }
        private bool IsLoggedIn()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Login").IsNullOrEmpty();
        }
        private int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }
    }
}
