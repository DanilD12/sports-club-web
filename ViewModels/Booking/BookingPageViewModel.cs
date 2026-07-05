namespace Stalika.Web.ViewModels.Booking;

public class BookingPageViewModel
{
    public DateTime SelectedDate { get; set; }
    public List<BookingTableViewModel> Tables { get; set; } = new();
}