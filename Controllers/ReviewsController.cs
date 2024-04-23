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
    public class ReviewsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewsController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            if (!IsAdminJoined() && review.UserId != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Cars");
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", review.ProductId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", review.UserId);
            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,ProductId,Comment,CreatedAt")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            var existingReviews = await _context.Reviews.FindAsync(id);

            if (existingReviews == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingReviews.UserId = review.UserId;
                    existingReviews.ProductId = review.ProductId;
                    existingReviews.Comment = review.Comment;
                    existingReviews.CreatedAt = DateTime.Now;

                    _context.Update(existingReviews);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Products", new { id = review!.ProductId });
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", review.ProductId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", review.UserId);
            return View(review);
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            if (!IsAdminJoined() && review.UserId != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Cars");
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Products", new { id = review!.ProductId });
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return ViewBag.Name != null && ViewBag.isAdmin == 1;
        }
        private int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }
    }
}
