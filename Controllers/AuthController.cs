using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stalika.Web.Data;
using Stalika.Web.Services;
using Stalika.Web.ViewModels.Auth;

namespace Stalika.Web.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            model.ErrorMessage = "Пользователь с таким email не найден.";
            return View(model);
        }

        bool isPasswordValid = PasswordHasher.VerifyPassword(model.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            model.ErrorMessage = "Неверный пароль.";
            return View(model);
        }

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

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Booking");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Password != model.ConfirmPassword)
        {
            model.ErrorMessage = "Пароли не совпадают.";
            return View(model);
        }

        bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
        if (emailExists)
        {
            model.ErrorMessage = "Пользователь с таким email уже существует.";
            return View(model);
        }

        var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "клиент");
        if (clientRole == null)
        {
            model.ErrorMessage = "Роль 'клиент' не найдена в базе.";
            return View(model);
        }

        var user = new Stalika.Web.Entities.User
        {
            FirstName = model.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone.Trim(),
            PasswordHash = PasswordHasher.HashPassword(model.Password),
            RoleId = clientRole.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, clientRole.RoleName)
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

        return RedirectToAction("Index", "Booking");
    }
}