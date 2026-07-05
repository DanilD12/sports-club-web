namespace Stalika.Web.ViewModels.Booking;

public class BookingSlotDetailsViewModel
{
    public int BookingNumber { get; set; }
    public int TableNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;

    public string TrainerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    public List<string> EquipmentItems { get; set; } = new();
}