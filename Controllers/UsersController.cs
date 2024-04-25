using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UsersController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(await _context.Users.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> MyPage()
        {
            if (IsLoggedIn())
            {
                string login = ViewBag.Login;
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login);
                var userReviews = await _context.Reviews
                    .Include(u => u.Product!.Brand)
                    .Where(u => u.UserId == user!.Id)
                    .ToListAsync();

                if (user == null)
                {
                    return BadRequest();
                }

                var userData = new UserViewModel
                {
                    User = user,
                    Reviews = userReviews
                };

                return View(userData);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Login()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("Id,Login,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            var isUserExist = await _context.Users.FirstOrDefaultAsync(
                u => u.Login == user.Login &&
                u.Password == user.Password);

            if (isUserExist == null)
            {
                ModelState.AddModelError("Login", "User with this login is not exist.");
                return View(user);
            }

            if (isUserExist != null && _httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.SetInt32("Id", isUserExist.Id);
                _httpContextAccessor.HttpContext.Session.SetString("Login", isUserExist.Login);
                _httpContextAccessor.HttpContext.Session.SetString("Role", isUserExist.Role.ToString());
                _httpContextAccessor.HttpContext.Session.SetString("Password", isUserExist.Password);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid login or password";
                return RedirectToAction(nameof(Login));
            }
        }

        // GET: Users/Register
        public IActionResult Register()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Users/Register
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Id,Role,Login,Password,Email")] User user)
        {
            if (_httpContextAccessor.HttpContext != null && ModelState.IsValid)
            {
                var isUserExist = await _context.Users.FirstOrDefaultAsync(
                    u => u.Login == user.Login);

                if (isUserExist != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                _httpContextAccessor.HttpContext.Session.SetInt32("Id", user.Id);
                _httpContextAccessor.HttpContext.Session.SetString("Login", user.Login);
                _httpContextAccessor.HttpContext.Session.SetString("Role", "User");
                _httpContextAccessor.HttpContext.Session.SetString("Password", user.Password);

                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Role,Login,Password,Email")] User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(
                    u => u.Login == user.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminJoined() && id != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Role,Login,Password,Email")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    var sameLoginUser = await _context.Users.FirstOrDefaultAsync(
                        u => u.Login == user.Login &&
                        u.Id != id);

                    if (sameLoginUser != null)
                    {
                        ModelState.AddModelError("Login", "User with this login already exists.");
                        return View(user);
                    }

                    existingUser.Role = user.Role;
                    existingUser.Login = user.Login;
                    existingUser.Password = user.Password;
                    existingUser.Email = user.Email;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();

                    if (existingUser.Id == _httpContextAccessor.HttpContext!.Session.GetInt32("Id"))
                    {
                        _httpContextAccessor.HttpContext!.Session.SetInt32("Id", user.Id);
                        _httpContextAccessor.HttpContext!.Session.SetString("Login", user.Login);
                        _httpContextAccessor.HttpContext!.Session.SetString("Password", user.Password);
                        _httpContextAccessor.HttpContext.Session.SetString("Role", user.Role.ToString());
                    }

                    if (user.Id == GetCurrentUserId() && !IsAdminJoined())
                    {
                        return RedirectToAction(nameof(MyPage));
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminJoined() && id != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                if (user.Login == _httpContextAccessor.HttpContext!.Session.GetString("Login"))
                {
                    _httpContextAccessor.HttpContext.Session.Remove("Id");
                    _httpContextAccessor.HttpContext.Session.Remove("Login");
                    _httpContextAccessor.HttpContext.Session.Remove("Password");
                    _httpContextAccessor.HttpContext.Session.Remove("Role");
                    ViewBag.Login = null;
                    ViewBag.Role = null;

                    return RedirectToAction("Index", "Home");
                }
            }
            return RedirectToAction(nameof(Index));
        }
        public IActionResult LogOut()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.Remove("Id");
                _httpContextAccessor.HttpContext.Session.Remove("Login");
                _httpContextAccessor.HttpContext.Session.Remove("Role");
                _httpContextAccessor.HttpContext.Session.Remove("Password");
                ViewBag.Login = null;
                ViewBag.Role = null;
            }
            return RedirectToAction("Index", "Home");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetString("Role") == "Admin";
        }
        private int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }
        private bool IsLoggedIn()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Login").IsNullOrEmpty();
        }
    }
}
