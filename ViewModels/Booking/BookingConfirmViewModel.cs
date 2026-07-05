using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stalika.Web.ViewModels.Booking;

public class BookingConfirmViewModel
{
    public int TableNumber { get; set; }
    public int GymNumber { get; set; }
    public string GymName { get; set; } = string.Empty;

    public decimal TablePricePerHour { get; set; }

    public DateTime BookingDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int? SelectedTrainerId { get; set; }

    public List<BookingTrainerItemViewModel> Trainers { get; set; } = new();
    public List<BookingEquipmentItemViewModel> EquipmentItems { get; set; } = new();

    public decimal TrainerPrice { get; set; }
    public decimal EquipmentTotal { get; set; }
    public decimal TotalPrice { get; set; }

    public decimal DurationHours { get; set; }
    public decimal SlotPrice { get; set; }

    public int? SelectedClientId { get; set; }
    public List<SelectListItem> AvailableClients { get; set; } = new();
}