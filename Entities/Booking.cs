namespace Stalika.Web.Entities;

public class Booking
{
    public int BookingNumber { get; set; }
    public int UserId { get; set; }
    public int TableNumber { get; set; }
    public DateTime StartTime { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? BookingDate { get; set; }
    public int? CoachId { get; set; }
    public DateTime EndTime { get; set; }

    public User? User { get; set; }
    public Table? Table { get; set; }
    public Trainer? Trainer { get; set; }

    public ICollection<EquipmentRental> EquipmentRentals { get; set; } = new List<EquipmentRental>();
}