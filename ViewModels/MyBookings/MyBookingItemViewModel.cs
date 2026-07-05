namespace Stalika.Web.ViewModels.MyBookings;

public class MyBookingItemViewModel
{
    public int BookingNumber { get; set; }
    public DateTime? BookingDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TableNumber { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    public List<string> EquipmentItems { get; set; } = new();
}