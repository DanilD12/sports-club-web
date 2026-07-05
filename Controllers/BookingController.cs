using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stalika.Web.Data;
using Stalika.Web.Entities;
using Stalika.Web.ViewModels.Booking;
using System.Security.Claims;

namespace Stalika.Web.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _context;

    public BookingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date)
    {
        var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var selectedDate = DateTime.SpecifyKind(
            (date ?? today).Date,
            DateTimeKind.Unspecified);

        if (selectedDate < today)
        {
            TempData["ErrorMessage"] = "Нельзя бронировать прошедшую дату.";
            return RedirectToAction(nameof(Index), new { date = today });
        }

        var dayStart = selectedDate;
        var dayEnd = selectedDate.AddDays(1);

        var tables = await _context.Tables
            .Include(t => t.Gym)
            .OrderBy(t => t.GymNumber)
            .ThenBy(t => t.TableNumber)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Where(b => b.StartTime >= dayStart && b.StartTime < dayEnd)
            .ToListAsync();

        var model = new BookingPageViewModel
        {
            SelectedDate = selectedDate,
            Tables = new List<BookingTableViewModel>()
        };

        foreach (var table in tables)
        {
            var openingTime = table.Gym?.OpeningTime ?? new TimeSpan(10, 0, 0);
            var closingTime = table.Gym?.ClosingTime ?? new TimeSpan(18, 0, 0);

            var tableVm = new BookingTableViewModel
            {
                TableNumber = table.TableNumber,
                GymNumber = table.GymNumber,
                PricePerHour = table.PricePerHour,
                OpeningTime = openingTime,
                ClosingTime = closingTime
            };

            for (var time = openingTime; time < closingTime; time = time.Add(TimeSpan.FromMinutes(30)))
            {
                var start = DateTime.SpecifyKind(
                    selectedDate.Add(time),
                    DateTimeKind.Unspecified);

                var end = DateTime.SpecifyKind(
                    selectedDate.Add(time).AddMinutes(30),
                    DateTimeKind.Unspecified);

                if (end.TimeOfDay > closingTime)
                    break;

                bool isBusy = bookings.Any(b =>
                    b.TableNumber == table.TableNumber &&
                    b.StartTime < end &&
                    b.EndTime > start);

                bool isPast = selectedDate == today && start <= now;

                tableVm.Slots.Add(new BookingSlotViewModel
                {
                    TableNumber = table.TableNumber,
                    StartTime = start,
                    EndTime = end,
                    TimeText = $"{start:HH:mm}-{end:HH:mm}",
                    IsPast = isPast,
                    IsAvailable = !isBusy && !isPast
                });
            }

            model.Tables.Add(tableVm);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int tableNumber, DateTime startTime, DateTime endTime, int? selectedTrainerId)
    {
        startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified);
        endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Unspecified);

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        if (startTime <= now)
        {
            TempData["ErrorMessage"] = "Нельзя бронировать прошедшее время.";
            return RedirectToAction(nameof(Index), new { date = startTime.Date });
        }

        var model = await BuildConfirmModelAsync(
            tableNumber,
            startTime,
            endTime,
            selectedTrainerId,
            null,
            null);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(BookingConfirmViewModel model)
    {
        int? currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Auth");

        int bookingUserId = currentUserId.Value;

        if (User.IsInRole("администратор"))
        {
            if (!model.SelectedClientId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Для администратора нужно выбрать клиента.");
            }
            else
            {
                bookingUserId = model.SelectedClientId.Value;
            }
        }

        var startTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Unspecified);
        var endTime = DateTime.SpecifyKind(model.EndTime, DateTimeKind.Unspecified);
        var bookingDate = DateTime.SpecifyKind(startTime.Date, DateTimeKind.Unspecified);

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        if (startTime <= now)
        {
            TempData["ErrorMessage"] = "Нельзя бронировать прошедшее время.";
            return RedirectToAction(nameof(Index), new { date = startTime.Date });
        }

        var rebuiltModel = await BuildConfirmModelAsync(
            model.TableNumber,
            startTime,
            endTime,
            model.SelectedTrainerId,
            model.SelectedClientId,
            model.EquipmentItems);

        if (rebuiltModel == null)
            return NotFound();

        model = rebuiltModel;

        bool isBusy = await _context.Bookings.AnyAsync(b =>
            b.TableNumber == model.TableNumber &&
            b.StartTime < endTime &&
            b.EndTime > startTime);

        if (isBusy)
        {
            ModelState.AddModelError(string.Empty, "Этот слот уже занят.");
            return View(model);
        }

        foreach (var item in model.EquipmentItems)
        {
            if (item.SelectedQuantity < 0)
            {
                ModelState.AddModelError(string.Empty, $"Количество для \"{item.EquipmentName}\" не может быть отрицательным.");
                return View(model);
            }

            if (item.SelectedQuantity > item.AvailableQuantity)
            {
                ModelState.AddModelError(string.Empty,
                    $"Для \"{item.EquipmentName}\" доступно только {item.AvailableQuantity} шт.");
                return View(model);
            }
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var booking = new Booking
            {
                UserId = bookingUserId,
                TableNumber = model.TableNumber,
                StartTime = startTime,
                EndTime = endTime,
                BookingDate = bookingDate,
                CoachId = model.SelectedTrainerId,
                TotalPrice = model.TotalPrice
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var rentals = model.EquipmentItems
                .Where(x => x.SelectedQuantity > 0)
                .Select(x => new EquipmentRental
                {
                    BookingNumber = booking.BookingNumber,
                    EquipmentName = x.EquipmentName,
                    Quantity = x.SelectedQuantity,
                    Amount = x.PricePerHour * x.SelectedQuantity * model.DurationHours
                })
                .ToList();

            if (rentals.Any())
            {
                _context.EquipmentRentals.AddRange(rentals);
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();

            TempData["SuccessMessage"] = "Бронирование подтверждено.";
            return RedirectToAction(nameof(Index), new { date = bookingDate.Date });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? "Не удалось сохранить бронирование.");
            return View(model);
        }
    }

    private async Task<BookingConfirmViewModel?> BuildConfirmModelAsync(
    int tableNumber,
    DateTime startTime,
    DateTime endTime,
    int? selectedTrainerId,
    int? selectedClientId,
    List<BookingEquipmentItemViewModel>? postedEquipment)
    {
        var table = await _context.Tables
            .Include(t => t.Gym)
            .FirstOrDefaultAsync(t => t.TableNumber == tableNumber);

        if (table == null)
            return null;

        var trainers = await (
            from t in _context.Trainers
            join u in _context.Users on t.UserId equals u.UserId
            orderby u.FirstName, u.LastName
            select new BookingTrainerItemViewModel
            {
                TrainerId = t.TrainerId,
                FullName = (u.FirstName + " " + (u.LastName ?? "")).Trim(),
                HourlyRate = t.HourlyRate ?? 0m,
                Qualification = t.Qualification ?? ""
            }).ToListAsync();

        var equipmentList = await _context.Equipment
            .OrderBy(e => e.Type)
            .ThenBy(e => e.EquipmentName)
            .ToListAsync();

        var equipmentItems = equipmentList
            .Select(eq =>
            {
                int selectedQty = postedEquipment?
                    .FirstOrDefault(x => x.EquipmentName == eq.EquipmentName)?
                    .SelectedQuantity ?? 0;

                return new BookingEquipmentItemViewModel
                {
                    EquipmentName = eq.EquipmentName,
                    Type = eq.Type,
                    AvailableQuantity = eq.Quantity,
                    PricePerHour = eq.PricePerHour,
                    SelectedQuantity = selectedQty
                };
            })
            .ToList();

        var clients = new List<SelectListItem>();

        if (User.IsInRole("администратор"))
        {
            var clientRoleId = await _context.Roles
                .Where(r => r.RoleName == "клиент")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            clients = await _context.Users
                .Where(u => u.RoleId == clientRoleId)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                })
                .ToListAsync();
        }

        var durationHours = (decimal)(endTime - startTime).TotalHours;
        var slotPrice = table.PricePerHour * durationHours;

        decimal trainerPrice = trainers
            .FirstOrDefault(t => t.TrainerId == selectedTrainerId)?
            .HourlyRate ?? 0m;

        trainerPrice *= durationHours;

        decimal equipmentTotal = equipmentItems
            .Where(x => x.SelectedQuantity > 0)
            .Sum(x => x.PricePerHour * x.SelectedQuantity * durationHours);

        return new BookingConfirmViewModel
        {
            TableNumber = table.TableNumber,
            GymNumber = table.GymNumber,
            GymName = table.Gym?.Name ?? $"Зал {table.GymNumber}",
            TablePricePerHour = table.PricePerHour,
            BookingDate = startTime.Date,
            StartTime = startTime,
            EndTime = endTime,
            SelectedTrainerId = selectedTrainerId,
            Trainers = trainers,
            EquipmentItems = equipmentItems,
            TrainerPrice = trainerPrice,
            EquipmentTotal = equipmentTotal,
            DurationHours = durationHours,
            SlotPrice = slotPrice,
            AvailableClients = clients,
            SelectedClientId = User.IsInRole("администратор") ? selectedClientId : GetCurrentUserId(),
            TotalPrice = slotPrice + trainerPrice + equipmentTotal,
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        int? userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var startTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Unspecified);
        var endTime = DateTime.SpecifyKind(model.EndTime, DateTimeKind.Unspecified);
        var bookingDate = DateTime.SpecifyKind(startTime.Date, DateTimeKind.Unspecified);

        bool isBusy = await _context.Bookings.AnyAsync(b =>
            b.TableNumber == model.TableNumber &&
            b.StartTime < endTime &&
            b.EndTime > startTime);

        if (isBusy)
        {
            TempData["ErrorMessage"] = "Этот слот уже занят.";
            return RedirectToAction(nameof(Index), new { date = startTime.Date });
        }

        var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == model.TableNumber);
        if (table == null)
        {
            TempData["ErrorMessage"] = "Стол не найден.";
            return RedirectToAction(nameof(Index), new { date = startTime.Date });
        }

        var booking = new Booking
        {
            UserId = userId.Value,
            TableNumber = model.TableNumber,
            StartTime = startTime,
            EndTime = endTime,
            BookingDate = bookingDate,
            TotalPrice = table.PricePerHour,
            CoachId = null
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Бронирование успешно создано.";
        return RedirectToAction(nameof(Index), new { date = startTime.Date });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int userId) ? userId : null;
    }

    [Authorize(Roles = "администратор")]
    [HttpGet]
    public async Task<IActionResult> SlotDetails(int tableNumber, DateTime startTime, DateTime endTime)
    {
        startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified);
        endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Unspecified);

        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Trainer)
                .ThenInclude(t => t.User)
            .Include(b => b.EquipmentRentals)
            .FirstOrDefaultAsync(b =>
                b.TableNumber == tableNumber &&
                b.StartTime < endTime &&
                b.EndTime > startTime);

        if (booking == null)
        {
            TempData["ErrorMessage"] = "На этот слот бронирование не найдено.";
            return RedirectToAction(nameof(Index), new { date = startTime.Date });
        }

        var model = new BookingSlotDetailsViewModel
        {
            BookingNumber = booking.BookingNumber,
            TableNumber = booking.TableNumber,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            ClientName = $"{booking.User?.FirstName} {booking.User?.LastName}".Trim(),
            ClientEmail = booking.User?.Email ?? "",
            ClientPhone = booking.User?.Phone ?? "",
            TrainerName = booking.Trainer?.User != null
                ? $"{booking.Trainer.User.FirstName} {booking.Trainer.User.LastName}".Trim()
                : "",
            TotalPrice = booking.TotalPrice,
            EquipmentItems = booking.EquipmentRentals
                .Select(x => $"{x.EquipmentName} x{x.Quantity} ({x.Amount:0.##} ₽)")
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrepareConfirm(BookingMultiConfirmViewModel model)
    {
        if (model.SelectedSlots == null || !model.SelectedSlots.Any())
        {
            TempData["ErrorMessage"] = "Выберите хотя бы один слот.";
            return RedirectToAction(nameof(Index));
        }

        var rebuiltModel = await BuildMultiConfirmModelAsync(
            model.SelectedSlots,
            null,
            null,
            null);

        if (rebuiltModel == null)
        {
            TempData["ErrorMessage"] = "Не удалось подготовить бронирование.";
            return RedirectToAction(nameof(Index));
        }

        return View("ConfirmMultiple", rebuiltModel);
    }

    private async Task<BookingMultiConfirmViewModel?> BuildMultiConfirmModelAsync(
    List<BookingSelectedSlotInputViewModel> selectedSlots,
    int? selectedTrainerId,
    int? selectedClientId,
    List<BookingEquipmentItemViewModel>? postedEquipment)
    {
        if (selectedSlots == null || !selectedSlots.Any())
            return null;

        foreach (var slot in selectedSlots)
        {
            slot.StartTime = DateTime.SpecifyKind(slot.StartTime, DateTimeKind.Unspecified);
            slot.EndTime = DateTime.SpecifyKind(slot.EndTime, DateTimeKind.Unspecified);
        }

        var trainers = await _context.Trainers
            .Include(t => t.User)
            .OrderBy(t => t.User!.FirstName)
            .ThenBy(t => t.User!.LastName)
            .Select(t => new BookingTrainerItemViewModel
            {
                TrainerId = t.TrainerId,
                FullName = ((t.User!.FirstName ?? "") + " " + (t.User!.LastName ?? "")).Trim(),
                HourlyRate = t.HourlyRate ?? 0m
            })
            .ToListAsync();

        var equipment = await _context.Equipment
            .OrderBy(e => e.Type)
            .ThenBy(e => e.EquipmentName)
            .ToListAsync();

        var equipmentItems = equipment.Select(e =>
        {
            var posted = postedEquipment?.FirstOrDefault(x => x.EquipmentName == e.EquipmentName);

            return new BookingEquipmentItemViewModel
            {
                EquipmentName = e.EquipmentName,
                Type = e.Type,
                AvailableQuantity = e.Quantity,
                PricePerHour = e.PricePerHour,
                SelectedQuantity = posted?.SelectedQuantity ?? 0
            };
        }).ToList();

        decimal totalDurationHours = selectedSlots.Sum(x => (decimal)(x.EndTime - x.StartTime).TotalHours);

        var tableNumbers = selectedSlots.Select(x => x.TableNumber).Distinct().ToList();

        var tables = await _context.Tables
            .Where(t => tableNumbers.Contains(t.TableNumber))
            .ToDictionaryAsync(t => t.TableNumber, t => t);

        decimal slotsTotalPrice = selectedSlots.Sum(slot =>
        {
            var table = tables[slot.TableNumber];
            var duration = (decimal)(slot.EndTime - slot.StartTime).TotalHours;
            return table.PricePerHour * duration;
        });

        decimal trainerTotal = trainers
            .FirstOrDefault(t => t.TrainerId == selectedTrainerId)?
            .HourlyRate ?? 0m;

        trainerTotal *= totalDurationHours;

        decimal equipmentTotal = equipmentItems
            .Where(x => x.SelectedQuantity > 0)
            .Sum(x => x.PricePerHour * x.SelectedQuantity * totalDurationHours);

        var clients = new List<SelectListItem>();

        if (User.IsInRole("администратор"))
        {
            var clientRoleId = await _context.Roles
                .Where(r => r.RoleName == "клиент")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            clients = await _context.Users
                .Where(u => u.RoleId == clientRoleId)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                })
                .ToListAsync();
        }

        return new BookingMultiConfirmViewModel
        {
            SelectedSlots = selectedSlots,
            SlotTexts = selectedSlots
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.TableNumber)
                .Select(x => $"Стол {x.TableNumber} | {x.StartTime:dd.MM.yyyy HH:mm}-{x.EndTime:HH:mm}")
                .ToList(),

            TotalDurationHours = totalDurationHours,
            SlotsTotalPrice = slotsTotalPrice,

            SelectedTrainerId = selectedTrainerId,
            Trainers = trainers,

            SelectedClientId = User.IsInRole("администратор") ? selectedClientId : GetCurrentUserId(),
            AvailableClients = clients,

            EquipmentItems = equipmentItems,
            TotalPrice = slotsTotalPrice + trainerTotal + equipmentTotal
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmMultiple(BookingMultiConfirmViewModel model)
    {
        if (model.SelectedSlots == null || !model.SelectedSlots.Any())
        {
            TempData["ErrorMessage"] = "Не выбраны слоты для бронирования.";
            return RedirectToAction(nameof(Index));
        }

        int? currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Auth");

        int bookingUserId = currentUserId.Value;

        if (User.IsInRole("администратор"))
        {
            if (!model.SelectedClientId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Выберите клиента.");
            }
            else
            {
                bookingUserId = model.SelectedClientId.Value;
            }
        }

        if (!ModelState.IsValid)
        {
            var rebuilt = await BuildMultiConfirmModelAsync(
                model.SelectedSlots,
                model.SelectedTrainerId,
                model.SelectedClientId,
                model.EquipmentItems);

            return View("ConfirmMultiple", rebuilt);
        }

        foreach (var slot in model.SelectedSlots)
        {
            slot.StartTime = DateTime.SpecifyKind(slot.StartTime, DateTimeKind.Unspecified);
            slot.EndTime = DateTime.SpecifyKind(slot.EndTime, DateTimeKind.Unspecified);
        }

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        foreach (var slot in model.SelectedSlots)
        {
            if (slot.StartTime <= now)
            {
                TempData["ErrorMessage"] = "Один из выбранных слотов уже недоступен.";
                return RedirectToAction(nameof(Index), new { date = slot.StartTime.Date });
            }

            bool isBusy = await _context.Bookings.AnyAsync(b =>
                b.TableNumber == slot.TableNumber &&
                b.StartTime < slot.EndTime &&
                b.EndTime > slot.StartTime);

            if (isBusy)
            {
                TempData["ErrorMessage"] = "Один из выбранных слотов уже занят.";
                return RedirectToAction(nameof(Index), new { date = slot.StartTime.Date });
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var slot in model.SelectedSlots)
            {
                var table = await _context.Tables.FirstAsync(t => t.TableNumber == slot.TableNumber);

                var durationHours = (decimal)(slot.EndTime - slot.StartTime).TotalHours;

                decimal trainerTotal = 0m;
                if (model.SelectedTrainerId.HasValue)
                {
                    var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.TrainerId == model.SelectedTrainerId.Value);
                    trainerTotal = (trainer?.HourlyRate ?? 0m) * durationHours;
                }

                decimal equipmentTotal = model.EquipmentItems
                    .Where(x => x.SelectedQuantity > 0)
                    .Sum(x => x.PricePerHour * x.SelectedQuantity * durationHours);

                var booking = new Booking
                {
                    UserId = bookingUserId,
                    TableNumber = slot.TableNumber,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    BookingDate = DateTime.SpecifyKind(slot.StartTime.Date, DateTimeKind.Unspecified),
                    CoachId = model.SelectedTrainerId,
                    TotalPrice = table.PricePerHour * durationHours + trainerTotal + equipmentTotal
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                var rentals = model.EquipmentItems
                    .Where(x => x.SelectedQuantity > 0)
                    .Select(x => new EquipmentRental
                    {
                        BookingNumber = booking.BookingNumber,
                        EquipmentName = x.EquipmentName,
                        Quantity = x.SelectedQuantity,
                        Amount = x.PricePerHour * x.SelectedQuantity * durationHours
                    })
                    .ToList();

                if (rentals.Any())
                {
                    _context.EquipmentRentals.AddRange(rentals);
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Бронирование успешно создано.";
            return RedirectToAction("Index", "MyBookings");
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Не удалось сохранить бронирование.";
            return RedirectToAction(nameof(Index));
        }
    }
}