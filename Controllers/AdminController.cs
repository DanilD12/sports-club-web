using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stalika.Web.Data;
using Stalika.Web.Entities;
using Stalika.Web.Services;
using Stalika.Web.ViewModels.Admin;
using ClosedXML.Excel;

namespace Stalika.Web.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.UsersCount = await _context.Users.CountAsync();
        ViewBag.BookingsCount = await _context.Bookings.CountAsync();
        ViewBag.TournamentsCount = await _context.Tournaments.CountAsync();
        ViewBag.EquipmentCount = await _context.Equipment.CountAsync();
        ViewBag.TrainersCount = await _context.Trainers.CountAsync();

        return View();
    }

    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .OrderByDescending(u => u.UserId)
            .ToListAsync();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Tournaments()
    {
        var tournaments = await _context.Tournaments
            .OrderBy(t => t.Date)
            .ToListAsync();

        var participants = await _context.TournamentParticipants.ToListAsync();

        ViewBag.ParticipantCounts = participants
            .GroupBy(x => x.TournamentId)
            .ToDictionary(g => g.Key, g => g.Count());

        return View(tournaments);
    }

    [HttpGet]
    public IActionResult CreateTournament()
    {
        var model = new AdminTournamentEditViewModel
        {
            Date = DateTime.Today,
            MaxParticipants = 16
        };

        return View("TournamentForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTournament(AdminTournamentEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("TournamentForm", model);

        var tournament = new Tournament
        {
            TournamentName = model.TournamentName.Trim(),
            Organizer = model.Organizer.Trim(),
            Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Unspecified),
            MaxParticipants = model.MaxParticipants,
            ParticipantCount = 0
        };

        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Турнир успешно создан.";
        return RedirectToAction(nameof(Tournaments));
    }

    [HttpGet]
    public async Task<IActionResult> EditTournament(int id)
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.TournamentId == id);
        if (tournament == null)
            return NotFound();

        var model = new AdminTournamentEditViewModel
        {
            TournamentId = tournament.TournamentId,
            TournamentName = tournament.TournamentName,
            Organizer = tournament.Organizer,
            Date = tournament.Date,
            MaxParticipants = tournament.MaxParticipants
        };

        return View("TournamentForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTournament(AdminTournamentEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("TournamentForm", model);

        if (model.TournamentId == null)
            return BadRequest();

        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.TournamentId == model.TournamentId.Value);
        if (tournament == null)
            return NotFound();

        int currentParticipants = await _context.TournamentParticipants.CountAsync(tp => tp.TournamentId == tournament.TournamentId);

        if (model.MaxParticipants < currentParticipants)
        {
            ModelState.AddModelError(nameof(model.MaxParticipants),
                $"Максимум участников не может быть меньше текущего количества ({currentParticipants}).");
            return View("TournamentForm", model);
        }

        tournament.TournamentName = model.TournamentName.Trim();
        tournament.Organizer = model.Organizer.Trim();
        tournament.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Unspecified);
        tournament.MaxParticipants = model.MaxParticipants;
        tournament.ParticipantCount = currentParticipants;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Турнир успешно обновлён.";
        return RedirectToAction(nameof(Tournaments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.TournamentId == id);
        if (tournament == null)
        {
            TempData["ErrorMessage"] = "Турнир не найден.";
            return RedirectToAction(nameof(Tournaments));
        }

        bool hasParticipants = await _context.TournamentParticipants.AnyAsync(tp => tp.TournamentId == id);
        if (hasParticipants)
        {
            TempData["ErrorMessage"] = "Нельзя удалить турнир, на который уже записаны участники.";
            return RedirectToAction(nameof(Tournaments));
        }

        _context.Tournaments.Remove(tournament);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Турнир удалён.";
        return RedirectToAction(nameof(Tournaments));
    }

    [HttpGet]
    public async Task<IActionResult> Equipment()
    {
        var equipment = await _context.Equipment
            .OrderBy(e => e.Type)
            .ThenBy(e => e.EquipmentName)
            .ToListAsync();

        return View(equipment);
    }

    [HttpGet]
    public IActionResult CreateEquipment()
    {
        var model = new AdminEquipmentEditViewModel();
        return View("EquipmentForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEquipment(AdminEquipmentEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("EquipmentForm", model);

        bool exists = await _context.Equipment.AnyAsync(e => e.EquipmentName == model.EquipmentName);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.EquipmentName), "Инвентарь с таким названием уже существует.");
            return View("EquipmentForm", model);
        }

        var item = new Equipment
        {
            EquipmentName = model.EquipmentName.Trim(),
            Type = model.Type.Trim(),
            Quantity = model.Quantity,
            PricePerHour = model.PricePerHour
        };

        _context.Equipment.Add(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Инвентарь успешно добавлен.";
        return RedirectToAction(nameof(Equipment));
    }

    [HttpGet]
    public async Task<IActionResult> EditEquipment(string id)
    {
        var item = await _context.Equipment.FirstOrDefaultAsync(e => e.EquipmentName == id);
        if (item == null)
            return NotFound();

        var model = new AdminEquipmentEditViewModel
        {
            EquipmentName = item.EquipmentName,
            Type = item.Type,
            Quantity = item.Quantity,
            PricePerHour = item.PricePerHour
        };

        ViewBag.IsEdit = true;
        return View("EquipmentForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEquipment(AdminEquipmentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.IsEdit = true;
            return View("EquipmentForm", model);
        }

        var item = await _context.Equipment.FirstOrDefaultAsync(e => e.EquipmentName == model.EquipmentName);
        if (item == null)
            return NotFound();

        item.Type = model.Type.Trim();
        item.Quantity = model.Quantity;
        item.PricePerHour = model.PricePerHour;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Инвентарь успешно обновлён.";
        return RedirectToAction(nameof(Equipment));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEquipment(string id)
    {
        var item = await _context.Equipment.FirstOrDefaultAsync(e => e.EquipmentName == id);
        if (item == null)
        {
            TempData["ErrorMessage"] = "Инвентарь не найден.";
            return RedirectToAction(nameof(Equipment));
        }

        bool hasRentals = await _context.EquipmentRentals.AnyAsync(r => r.EquipmentName == id);
        if (hasRentals)
        {
            TempData["ErrorMessage"] = "Нельзя удалить инвентарь, который уже использовался в аренде.";
            return RedirectToAction(nameof(Equipment));
        }

        _context.Equipment.Remove(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Инвентарь удалён.";
        return RedirectToAction(nameof(Equipment));
    }

    [HttpGet]
    public async Task<IActionResult> Trainers()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .OrderBy(t => t.TrainerId)
            .ToListAsync();

        return View(trainers);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTrainer()
    {
        var model = new AdminTrainerEditViewModel
        {
            HourlyRate = 0
        };

        await FillAvailableUsersAsync(model);
        return View("TrainerForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTrainer(AdminTrainerEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillAvailableUsersAsync(model);
            return View("TrainerForm", model);
        }

        bool alreadyTrainer = await _context.Trainers.AnyAsync(t => t.UserId == model.UserId);
        if (alreadyTrainer)
        {
            ModelState.AddModelError(nameof(model.UserId), "Этот пользователь уже назначен тренером.");
            await FillAvailableUsersAsync(model);
            return View("TrainerForm", model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId);
        if (user == null)
        {
            ModelState.AddModelError(nameof(model.UserId), "Пользователь не найден.");
            await FillAvailableUsersAsync(model);
            return View("TrainerForm", model);
        }

        var trainerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleNames.Trainer);
        if (trainerRole == null)
        {
            ModelState.AddModelError(string.Empty, "Роль 'тренер' не найдена в базе.");
            await FillAvailableUsersAsync(model);
            return View("TrainerForm", model);
        }

        var trainer = new Trainer
        {
            UserId = model.UserId,
            HourlyRate = model.HourlyRate,
            Qualification = string.IsNullOrWhiteSpace(model.Qualification) ? null : model.Qualification.Trim()
        };

        user.RoleId = trainerRole.Id;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Тренер успешно добавлен.";
        return RedirectToAction(nameof(Trainers));
    }

    [HttpGet]
    public async Task<IActionResult> EditTrainer(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TrainerId == id);

        if (trainer == null)
            return NotFound();

        var model = new AdminTrainerEditViewModel
        {
            TrainerId = trainer.TrainerId,
            UserId = trainer.UserId,
            HourlyRate = trainer.HourlyRate ?? 0,
            Qualification = trainer.Qualification
        };

        await FillAvailableUsersAsync(model, includeCurrentUserId: trainer.UserId);
        ViewBag.IsEdit = true;
        ViewBag.CurrentUserName = $"{trainer.User?.FirstName} {trainer.User?.LastName}".Trim();

        return View("TrainerForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTrainer(AdminTrainerEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillAvailableUsersAsync(model, includeCurrentUserId: model.UserId);
            ViewBag.IsEdit = true;
            return View("TrainerForm", model);
        }

        if (model.TrainerId == null)
            return BadRequest();

        var trainer = await _context.Trainers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TrainerId == model.TrainerId.Value);

        if (trainer == null)
            return NotFound();

        trainer.HourlyRate = model.HourlyRate;
        trainer.Qualification = string.IsNullOrWhiteSpace(model.Qualification) ? null : model.Qualification.Trim();

        if (trainer.User != null)
            trainer.User.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Данные тренера обновлены.";
        return RedirectToAction(nameof(Trainers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTrainer(int id)
    {
        var trainer = await _context.Trainers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TrainerId == id);

        if (trainer == null)
        {
            TempData["ErrorMessage"] = "Тренер не найден.";
            return RedirectToAction(nameof(Trainers));
        }

        bool usedInBookings = await _context.Bookings.AnyAsync(b => b.CoachId == id);
        if (usedInBookings)
        {
            TempData["ErrorMessage"] = "Нельзя удалить тренера, который уже используется в бронированиях.";
            return RedirectToAction(nameof(Trainers));
        }

        var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleNames.Client);

        if (trainer.User != null && clientRole != null)
        {
            trainer.User.RoleId = clientRole.Id;
            trainer.User.UpdatedAt = DateTime.UtcNow;
        }

        _context.Trainers.Remove(trainer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Тренер удалён.";
        return RedirectToAction(nameof(Trainers));
    }

    private async Task FillAvailableUsersAsync(AdminTrainerEditViewModel model, int? includeCurrentUserId = null)
    {
        var trainerUserIds = await _context.Trainers
            .Select(t => t.UserId)
            .ToListAsync();

        var users = await _context.Users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        model.AvailableUsers = users
            .Where(u => !trainerUserIds.Contains(u.UserId) || u.UserId == includeCurrentUserId)
            .Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = $"{u.FirstName} {u.LastName} ({u.Email})"
            })
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Analytics()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalBookings = await _context.Bookings.CountAsync();
        var totalTournaments = await _context.Tournaments.CountAsync();
        var totalTrainers = await _context.Trainers.CountAsync();
        var totalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;

        var topTables = await _context.Bookings
            .GroupBy(b => b.TableNumber)
            .Select(g => new AdminTopTableItemViewModel
            {
                TableNumber = g.Key,
                BookingCount = g.Count()
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(5)
            .ToListAsync();

        var lowInventoryItems = await _context.Equipment
            .Where(e => e.Quantity <= 3)
            .OrderBy(e => e.Quantity)
            .Select(e => new AdminLowInventoryItemViewModel
            {
                EquipmentName = e.EquipmentName,
                Quantity = e.Quantity,
                Type = e.Type
            })
            .ToListAsync();

        var topClients = await _context.Bookings
            .GroupBy(b => b.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                BookingCount = g.Count(),
                TotalSpent = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(5)
            .Join(
                _context.Users,
                bookingGroup => bookingGroup.UserId,
                user => user.UserId,
                (bookingGroup, user) => new AdminTopClientItemViewModel
                {
                    UserId = user.UserId,
                    FullName = (user.FirstName + " " + (user.LastName ?? "")).Trim(),
                    BookingCount = bookingGroup.BookingCount,
                    TotalSpent = bookingGroup.TotalSpent
                })
            .ToListAsync();

        var today = DateTime.Today;
        var startDate = today.AddDays(-6);

        var bookingsRaw = await _context.Bookings
            .Where(b => b.BookingDate.HasValue && b.BookingDate.Value.Date >= startDate && b.BookingDate.Value.Date <= today)
            .ToListAsync();

        var bookingsByDay = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day = startDate.AddDays(i).Date;
                return new AdminChartPointViewModel
                {
                    Label = day.ToString("dd.MM"),
                    Value = bookingsRaw.Count(b => b.BookingDate.HasValue && b.BookingDate.Value.Date == day)
                };
            })
            .ToList();

        var revenueByDay = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day = startDate.AddDays(i).Date;
                return new AdminChartPointViewModel
                {
                    Label = day.ToString("dd.MM"),
                    Value = bookingsRaw
                        .Where(b => b.BookingDate.HasValue && b.BookingDate.Value.Date == day)
                        .Sum(b => b.TotalPrice)
                };
            })
            .ToList();

        var popularEquipment = await _context.EquipmentRentals
            .GroupBy(r => r.EquipmentName)
            .Select(g => new AdminPopularEquipmentItemViewModel
            {
                EquipmentName = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToListAsync();

        var trainerStats = await _context.Bookings
            .Where(b => b.CoachId != null)
            .GroupBy(b => b.CoachId)
            .Select(g => new
            {
                TrainerId = g.Key!.Value,
                BookingCount = g.Count(),
                TotalRevenue = g.Sum(x => x.TotalPrice)
            })
            .Join(
                _context.Trainers.Include(t => t.User),
                stats => stats.TrainerId,
                trainer => trainer.TrainerId,
                (stats, trainer) => new AdminTrainerStatsItemViewModel
                {
                    TrainerId = trainer.TrainerId,
                    FullName = ((trainer.User!.FirstName ?? "") + " " + (trainer.User!.LastName ?? "")).Trim(),
                    BookingCount = stats.BookingCount,
                    TotalRevenue = stats.TotalRevenue
                })
            .OrderByDescending(x => x.BookingCount)
            .ToListAsync();

        var model = new AdminAnalyticsViewModel
        {
            TotalUsers = totalUsers,
            TotalBookings = totalBookings,
            TotalTournaments = totalTournaments,
            TotalTrainers = totalTrainers,
            TotalRevenue = totalRevenue,
            TopTables = topTables,
            LowInventoryItems = lowInventoryItems,
            TopClients = topClients,
            BookingsByDay = bookingsByDay,
            RevenueByDay = revenueByDay,
            PopularEquipment = popularEquipment,
            TrainerStats = trainerStats
        };

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Bookings(string? search, string? trainerName, int? tableNumber, DateTime? date)
    {
        var model = await BuildAdminBookingsPageModelAsync(search, trainerName, tableNumber, date);
        return View(model);
    }

    private async Task<AdminBookingsPageViewModel> BuildAdminBookingsPageModelAsync(
    string? search,
    string? trainerName,
    int? tableNumber,
    DateTime? date)
    {
        var query = _context.BookingInfoRows.AsQueryable();

        if (date.HasValue)
        {
            var selectedDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Unspecified);
            var nextDate = selectedDate.AddDays(1);

            query = query.Where(x =>
                x.BookingDate.HasValue &&
                x.BookingDate.Value >= selectedDate &&
                x.BookingDate.Value < nextDate);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.ClientName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(trainerName))
        {
            query = query.Where(x => x.TrainerName == trainerName);
        }

        if (tableNumber.HasValue)
        {
            query = query.Where(x => x.TableNumber == tableNumber.Value);
        }

        var rawRows = await query
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .ToListAsync();

        var items = rawRows
            .GroupBy(x => new
            {
                x.BookingNumber,
                x.BookingDate,
                x.StartTime,
                x.EndTime,
                x.TableNumber,
                x.ClientName,
                x.TrainerName,
                x.TotalPrice
            })
            .Select(g => new AdminBookingItemViewModel
            {
                BookingNumber = g.Key.BookingNumber,
                BookingDate = g.Key.BookingDate,
                StartTime = g.Key.StartTime,
                EndTime = g.Key.EndTime,
                TableNumber = g.Key.TableNumber,
                ClientName = g.Key.ClientName,
                TrainerName = g.Key.TrainerName,
                TotalPrice = g.Key.TotalPrice,
                EquipmentItems = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.EquipmentName))
                    .Select(x => $"{x.EquipmentName} x{x.EquipmentQuantity} ({x.EquipmentAmount:0.##} ₽)")
                    .Distinct()
                    .ToList()
            })
            .ToList();

        var trainers = await _context.BookingInfoRows
            .Where(x => x.TrainerName != null && x.TrainerName != "")
            .Select(x => x.TrainerName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var tables = await _context.Tables
            .OrderBy(x => x.TableNumber)
            .Select(x => x.TableNumber)
            .ToListAsync();

        return new AdminBookingsPageViewModel
        {
            Search = search,
            TrainerName = trainerName,
            TableNumber = tableNumber,
            Date = date,
            Trainers = trainers,
            Tables = tables,
            Items = items
        };
    }

    [HttpGet]
    public async Task<IActionResult> ExportBookings(string? search, string? trainerName, int? tableNumber, DateTime? date)
    {
        var model = await BuildAdminBookingsPageModelAsync(search, trainerName, tableNumber, date);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Бронирования");

        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Дата";
        worksheet.Cell(1, 3).Value = "Время";
        worksheet.Cell(1, 4).Value = "Стол";
        worksheet.Cell(1, 5).Value = "Клиент";
        worksheet.Cell(1, 6).Value = "Тренер";
        worksheet.Cell(1, 7).Value = "Сумма";
        worksheet.Cell(1, 8).Value = "Инвентарь";

        int row = 2;

        foreach (var item in model.Items)
        {
            worksheet.Cell(row, 1).Value = item.BookingNumber;
            worksheet.Cell(row, 2).Value = item.BookingDate?.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 3).Value = $"{item.StartTime:HH:mm} - {item.EndTime:HH:mm}";
            worksheet.Cell(row, 4).Value = item.TableNumber;
            worksheet.Cell(row, 5).Value = item.ClientName;
            worksheet.Cell(row, 6).Value = string.IsNullOrWhiteSpace(item.TrainerName) ? "—" : item.TrainerName;
            worksheet.Cell(row, 7).Value = item.TotalPrice;
            worksheet.Cell(row, 8).Value = item.EquipmentItems.Any()
                ? string.Join(", ", item.EquipmentItems)
                : "—";

            row++;
        }

        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        worksheet.Column(1).Width = 10;
        worksheet.Column(2).Width = 14;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 10;
        worksheet.Column(5).Width = 28;
        worksheet.Column(6).Width = 28;
        worksheet.Column(7).Width = 14;
        worksheet.Column(8).Width = 60;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"bookings_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int bookingNumber)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);

        if (booking == null)
        {
            TempData["ErrorMessage"] = "Бронирование не найдено.";
            return RedirectToAction(nameof(Bookings));
        }

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Бронирование отменено.";
        return RedirectToAction(nameof(Bookings));
    }
}