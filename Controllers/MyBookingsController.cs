using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stalika.Web.Data;
using Stalika.Web.ViewModels.MyBookings;

namespace Stalika.Web.Controllers;

[Authorize]
public class MyBookingsController : Controller
{
    private readonly AppDbContext _context;

    public MyBookingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var rawRows = await (
            from b in _context.Bookings
            join bi in _context.BookingInfoRows on b.BookingNumber equals bi.BookingNumber
            where b.UserId == userId.Value
            orderby bi.BookingDate descending, bi.StartTime
            select bi
        ).ToListAsync();

        var model = rawRows
            .GroupBy(x => new
            {
                x.BookingNumber,
                x.BookingDate,
                x.StartTime,
                x.EndTime,
                x.TableNumber,
                x.TrainerName,
                x.TotalPrice
            })
            .Select(g => new MyBookingItemViewModel
            {
                BookingNumber = g.Key.BookingNumber,
                BookingDate = g.Key.BookingDate,
                StartTime = g.Key.StartTime,
                EndTime = g.Key.EndTime,
                TableNumber = g.Key.TableNumber,
                TrainerName = g.Key.TrainerName,
                TotalPrice = g.Key.TotalPrice,
                EquipmentItems = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.EquipmentName))
                    .Select(x => $"{x.EquipmentName} x{x.EquipmentQuantity} ({x.EquipmentAmount:0.##} ₽)")
                    .Distinct()
                    .ToList()
            })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int bookingNumber)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber && b.UserId == userId.Value);

        if (booking == null)
        {
            TempData["ErrorMessage"] = "Бронирование не найдено.";
            return RedirectToAction(nameof(Index));
        }

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Бронирование отменено.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }
}