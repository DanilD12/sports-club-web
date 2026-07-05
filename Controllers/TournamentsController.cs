using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stalika.Web.Data;
using Stalika.Web.Entities;
using Stalika.Web.ViewModels.Tournaments;
using System.Security.Claims;

namespace Stalika.Web.Controllers;

[Authorize]
public class TournamentsController : Controller
{
    private readonly AppDbContext _context;

    public TournamentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var tournaments = await _context.Tournaments
            .OrderBy(t => t.Date)
            .ToListAsync();

        var participants = await _context.TournamentParticipants.ToListAsync();

        var model = tournaments
            .Select(t =>
            {
                int currentCount = participants.Count(p => p.TournamentId == t.TournamentId);
                bool isRegistered = participants.Any(p =>
                    p.TournamentId == t.TournamentId &&
                    p.UserId == userId.Value);

                int freePlaces = t.MaxParticipants - currentCount;
                if (freePlaces < 0)
                    freePlaces = 0;

                return new TournamentItemViewModel
                {
                    TournamentId = t.TournamentId,
                    TournamentName = t.TournamentName,
                    Organizer = t.Organizer,
                    Date = t.Date,
                    CurrentParticipants = currentCount,
                    MaxParticipants = t.MaxParticipants,
                    FreePlaces = freePlaces,
                    IsUserRegistered = isRegistered,
                    CanRegister = !isRegistered && freePlaces > 0
                };
            })
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int tournamentId)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

        if (tournament == null)
        {
            TempData["ErrorMessage"] = "Турнир не найден.";
            return RedirectToAction(nameof(Index));
        }
        var today = DateTime.Today;

        if (tournament.Date.Date < today)
        {
            TempData["ErrorMessage"] = "Нельзя записаться на уже прошедший турнир.";
            return RedirectToAction(nameof(Index));
        }

        bool alreadyRegistered = await _context.TournamentParticipants.AnyAsync(tp =>
            tp.TournamentId == tournamentId &&
            tp.UserId == userId.Value);

        if (alreadyRegistered)
        {
            TempData["ErrorMessage"] = "Вы уже записаны на этот турнир.";
            return RedirectToAction(nameof(Index));
        }

        int currentCount = await _context.TournamentParticipants.CountAsync(tp =>
            tp.TournamentId == tournamentId);

        if (currentCount >= tournament.MaxParticipants)
        {
            TempData["ErrorMessage"] = "Свободных мест больше нет.";
            return RedirectToAction(nameof(Index));
        }

        var participant = new TournamentParticipant
        {
            UserId = userId.Value,
            TournamentId = tournamentId,
            Place = null
        };

        try
        {
            _context.TournamentParticipants.Add(participant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вы успешно записались на турнир.";
        }
        catch (DbUpdateException ex)
        {
            TempData["ErrorMessage"] = ex.InnerException?.Message ?? "Не удалось записаться на турнир.";
        }

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }

    [Authorize(Roles = "администратор")]
    [HttpGet]
    public async Task<IActionResult> Participants(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

        if (tournament == null)
            return NotFound();

        var participants = await _context.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Join(
                _context.Users.Include(u => u.Role),
                tp => tp.UserId,
                u => u.UserId,
                (tp, u) => new TournamentParticipantItemViewModel
                {
                    UserId = u.UserId,
                    FullName = (u.FirstName + " " + (u.LastName ?? "")).Trim(),
                    Email = u.Email,
                    Phone = u.Phone ?? "",
                    RoleName = u.Role != null ? u.Role.RoleName : "",
                    Place = tp.Place
                })
            .OrderBy(x => x.FullName)
            .ToListAsync();

        var model = new TournamentParticipantsPageViewModel
        {
            TournamentId = tournament.TournamentId,
            TournamentName = tournament.TournamentName,
            Date = tournament.Date,
            Participants = participants
        };

        return View(model);
    }

    [Authorize(Roles = "администратор")]
    [HttpGet]
    public async Task<IActionResult> RegisterClient(int tournamentId)
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.TournamentId == tournamentId);
        if (tournament == null)
            return NotFound();

        var clientRoleId = await _context.Roles
            .Where(r => r.RoleName == "клиент")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var existingIds = await _context.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        var clients = await _context.Users
            .Where(u => u.RoleId == clientRoleId && !existingIds.Contains(u.UserId))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.FirstName} {u.LastName} ({u.Email})"
            })
            .ToListAsync();

        var model = new TournamentRegisterClientViewModel
        {
            TournamentId = tournament.TournamentId,
            TournamentName = tournament.TournamentName,
            Date = tournament.Date,
            AvailableClients = clients
        };

        return View(model);
    }

    [Authorize(Roles = "администратор")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterClient(TournamentRegisterClientViewModel model)
    {
        if (!model.SelectedUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Выберите клиента.";
            return RedirectToAction(nameof(RegisterClient), new { tournamentId = model.TournamentId });
        }

        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.TournamentId == model.TournamentId);
        if (tournament == null)
            return NotFound();

        bool alreadyRegistered = await _context.TournamentParticipants.AnyAsync(tp =>
            tp.TournamentId == model.TournamentId &&
            tp.UserId == model.SelectedUserId.Value);

        if (alreadyRegistered)
        {
            TempData["ErrorMessage"] = "Этот клиент уже записан.";
            return RedirectToAction(nameof(Participants), new { tournamentId = model.TournamentId });
        }

        int currentCount = await _context.TournamentParticipants.CountAsync(tp => tp.TournamentId == model.TournamentId);

        if (currentCount >= tournament.MaxParticipants)
        {
            TempData["ErrorMessage"] = "Свободных мест больше нет.";
            return RedirectToAction(nameof(Index));
        }

        var participant = new TournamentParticipant
        {
            TournamentId = model.TournamentId,
            UserId = model.SelectedUserId.Value,
            Place = null
        };

        _context.TournamentParticipants.Add(participant);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Клиент записан на турнир.";
        return RedirectToAction(nameof(Participants), new { tournamentId = model.TournamentId });
    }

}