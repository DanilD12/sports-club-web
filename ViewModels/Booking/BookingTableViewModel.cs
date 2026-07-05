namespace Stalika.Web.ViewModels.Booking;

public class BookingTableViewModel
{
    public int TableNumber { get; set; }
    public int GymNumber { get; set; }
    public decimal PricePerHour { get; set; }

    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }

    public List<BookingSlotViewModel> Slots { get; set; } = new();
}