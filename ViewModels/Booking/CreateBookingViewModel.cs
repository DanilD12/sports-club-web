using System.ComponentModel.DataAnnotations;

namespace Stalika.Web.ViewModels.Booking;

public class CreateBookingViewModel
{
    [Required]
    public int TableNumber { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}