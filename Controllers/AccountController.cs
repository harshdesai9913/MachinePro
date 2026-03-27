using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MachinePro.Data;
using MachinePro.Models;
using System.Security.Claims;

namespace MachinePro.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db) => _db = db;

    // ─── LOGIN ───
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter username and password.";
            return View();
        }

        var user = await _db.AppUsers.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == username.ToLower().Trim() && u.Password == password);

        if (user == null)
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName),
            new Claim("UserId", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ─── LOGOUT ───
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ─── USER MANAGEMENT (Manager only) ───
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Users()
    {
        var users = await _db.AppUsers.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
        ViewBag.CurrentPage = "users";
        return View(users);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> AddUser(string username, string password, string fullName, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(role))
        {
            TempData["Error"] = "All fields are required.";
            return RedirectToAction("Users");
        }

        if (await _db.AppUsers.AnyAsync(u => u.Username.ToLower() == username.ToLower().Trim()))
        {
            TempData["Error"] = $"Username '{username}' already exists.";
            return RedirectToAction("Users");
        }

        _db.AppUsers.Add(new AppUser
        {
            Username = username.Trim().ToLower(),
            Password = password,
            FullName = fullName.Trim(),
            Role = role
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"User '{fullName}' added as {role}.";
        return RedirectToAction("Users");
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user != null)
        {
            // Prevent deleting yourself
            var currentUserId = User.FindFirst("UserId")?.Value;
            if (currentUserId == id.ToString())
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }
            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Users");
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "Password cannot be empty.";
            return RedirectToAction("Users");
        }

        var user = await _db.AppUsers.FindAsync(id);
        if (user != null)
        {
            user.Password = newPassword;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Password reset for {user.FullName}.";
        }
        return RedirectToAction("Users");
    }
}
