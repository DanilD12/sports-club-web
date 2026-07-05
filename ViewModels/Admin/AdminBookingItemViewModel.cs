namespace Stalika.Web.ViewModels.Admin;

public class AdminBookingItemViewModel
{
    public int BookingNumber { get; set; }
    public DateTime? BookingDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int TableNumber { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    public List<string> EquipmentItems { get; set; } = new();
}