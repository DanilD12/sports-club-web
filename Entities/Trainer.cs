namespace Stalika.Web.Entities;

public class Trainer
{
    public int TrainerId { get; set; }
    public int UserId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Qualification { get; set; }

    public User? User { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}