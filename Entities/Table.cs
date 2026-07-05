namespace Stalika.Web.Entities;

public class Table
{
    public int TableNumber { get; set; }
    public int GymNumber { get; set; }
    public decimal PricePerHour { get; set; }

    public Gym? Gym { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}