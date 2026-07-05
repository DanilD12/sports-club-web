namespace Stalika.Web.ViewModels.Booking;

public class BookingSlotViewModel
{
    public int TableNumber { get; set; }
    public string TimeText { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public bool IsAvailable { get; set; }
    public bool IsPast { get; set; }
}