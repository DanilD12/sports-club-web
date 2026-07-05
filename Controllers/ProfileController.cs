using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stalika.Web.Data;
using Stalika.Web.Services;
using Stalika.Web.ViewModels.Profile;

namespace Stalika.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
            return NotFound();

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            RoleName = user.Role?.RoleName ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
            return NotFound();

        model.RoleName = user.Role?.RoleName ?? string.Empty;

        if (!ModelState.IsValid)
            return View(model);

        bool emailBusy = await _context.Users
            .AnyAsync(u => u.Email == model.Email && u.UserId != user.UserId);

        if (emailBusy)
        {
            ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email уже существует.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword) ||
            !string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Введите новый пароль.");
                return View(model);
            }

            if (model.NewPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Пароль должен быть не короче 6 символов.");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Пароли не совпадают.");
                return View(model);
            }

            user.PasswordHash = PasswordHasher.HashPassword(model.NewPassword);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName.Trim();
        user.Email = model.Email.Trim();
        user.Phone = model.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RefreshAuthCookieAsync(user);

        TempData["SuccessMessage"] = "Профиль успешно сохранён.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }

    private async Task RefreshAuthCookieAsync(Stalika.Web.Entities.User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}