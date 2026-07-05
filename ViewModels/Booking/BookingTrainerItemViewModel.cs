namespace Stalika.Web.ViewModels.Booking;

public class BookingTrainerItemViewModel
{
    public int TrainerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public string Qualification { get; set; } = string.Empty;
}