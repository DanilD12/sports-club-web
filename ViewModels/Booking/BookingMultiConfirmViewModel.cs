using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stalika.Web.ViewModels.Booking;

public class BookingMultiConfirmViewModel
{
    public List<BookingSelectedSlotInputViewModel> SelectedSlots { get; set; } = new();

    public List<string> SlotTexts { get; set; } = new();

    public decimal TotalDurationHours { get; set; }
    public decimal SlotsTotalPrice { get; set; }

    public int? SelectedTrainerId { get; set; }
    public List<BookingTrainerItemViewModel> Trainers { get; set; } = new();

    public List<BookingEquipmentItemViewModel> EquipmentItems { get; set; } = new();

    public int? SelectedClientId { get; set; }
    public List<SelectListItem> AvailableClients { get; set; } = new();

    public decimal TotalPrice { get; set; }
}